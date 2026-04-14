import { Pipe, PipeTransform, SecurityContext, inject } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

/**
 * Minimal, sanitising Markdown-to-HTML pipe. Handles only the subset our briefings
 * actually emit: ATX headings (#..######), unordered lists (- / *), ordered lists
 * (1. ...), blank-line paragraphs, and inline `**bold**`/`*italic*`/`` `code` ``.
 *
 * Output is escaped first, then a small set of allowed tags is reintroduced, then
 * passed through Angular's sanitiser as belt-and-braces. We deliberately avoid
 * the full marked + dompurify pair to keep bundle size small.
 */
@Pipe({ name: 'markdown', standalone: true })
export class MarkdownPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(input: string | null | undefined): string {
    if (!input) return '';
    const html = renderMarkdown(input);
    return this.sanitizer.sanitize(SecurityContext.HTML, html) ?? '';
  }
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function renderInline(line: string): string {
  // Order matters: code first to avoid * inside code spans being interpreted.
  const escaped = escapeHtml(line);
  // Inline code: `text`
  let out = escaped.replace(/`([^`]+)`/g, '<code>$1</code>');
  // Bold: **text**
  out = out.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
  // Italic: *text* (single-star, non-greedy, not adjacent to space-asterisk)
  out = out.replace(/(^|[^*])\*([^*\s][^*]*[^\s*]|\S)\*(?!\*)/g, '$1<em>$2</em>');
  return out;
}

export function renderMarkdown(src: string): string {
  const lines = src.replace(/\r\n/g, '\n').split('\n');
  const blocks: string[] = [];
  let i = 0;

  while (i < lines.length) {
    const line = lines[i];
    if (line.trim() === '') {
      i++;
      continue;
    }

    // ATX headings
    const headingMatch = /^(#{1,6})\s+(.*)$/.exec(line);
    if (headingMatch) {
      const level = headingMatch[1].length;
      blocks.push(`<h${level}>${renderInline(headingMatch[2].trim())}</h${level}>`);
      i++;
      continue;
    }

    // Unordered list block
    if (/^\s*[-*]\s+/.test(line)) {
      const items: string[] = [];
      while (i < lines.length && /^\s*[-*]\s+/.test(lines[i])) {
        const item = lines[i].replace(/^\s*[-*]\s+/, '');
        items.push(`<li>${renderInline(item)}</li>`);
        i++;
      }
      blocks.push(`<ul>${items.join('')}</ul>`);
      continue;
    }

    // Ordered list block
    if (/^\s*\d+\.\s+/.test(line)) {
      const items: string[] = [];
      while (i < lines.length && /^\s*\d+\.\s+/.test(lines[i])) {
        const item = lines[i].replace(/^\s*\d+\.\s+/, '');
        items.push(`<li>${renderInline(item)}</li>`);
        i++;
      }
      blocks.push(`<ol>${items.join('')}</ol>`);
      continue;
    }

    // Paragraph: collapse contiguous non-empty, non-block lines
    const paragraphLines: string[] = [];
    while (
      i < lines.length &&
      lines[i].trim() !== '' &&
      !/^(#{1,6})\s+/.test(lines[i]) &&
      !/^\s*[-*]\s+/.test(lines[i]) &&
      !/^\s*\d+\.\s+/.test(lines[i])
    ) {
      paragraphLines.push(lines[i].trim());
      i++;
    }
    blocks.push(`<p>${renderInline(paragraphLines.join(' '))}</p>`);
  }

  return blocks.join('\n');
}
