/**
 * Generates a bookmarklet `javascript:` URL that imports the current Google Doc
 * as a Transcript capture into Mental Metal.
 *
 * The bookmarklet is self-contained — no external scripts, no localStorage, no popups.
 * The instance URL and PAT are baked in at generation time.
 */
export function generateBookmarkletUrl(instanceUrl: string, pat: string): string {
  // The bookmarklet source as a readable template.
  // Variables __INSTANCE_URL__ and __PAT__ are replaced with the user's values.
  const source = `
(function(){
  var u='__INSTANCE_URL__',t='__PAT__';
  if(location.hostname!=='docs.google.com'){s('Not a Google Doc','red');return}
  var m=location.href.match(/\\/document\\/(?:u\\/\\d+\\/)?d\\/([a-zA-Z0-9_-]+)/);
  if(!m){s('Not a Google Doc','red');return}
  var id=m[1],ctrl=new AbortController();
  setTimeout(function(){ctrl.abort()},30000);
  fetch('/document/d/'+id+'/export?format=txt',{signal:ctrl.signal})
  .then(function(r){if(!r.ok)throw new Error('Export failed: '+r.status);return r.text()})
  .then(function(c){
    if(c.length>10485760)throw new Error('Document too large');
    var title=(document.title||'').replace(/ - Google Docs$/,'').trim();
    return fetch(u+'/api/captures/import',{
      method:'POST',
      headers:{'Authorization':'Bearer '+t,'Content-Type':'application/json'},
      body:JSON.stringify({type:'Transcript',content:c,title:title,sourceUrl:location.href})
    })
  })
  .then(function(r){
    if(!r.ok)throw new Error(r.status===401?'401 Unauthorized - check your token in Settings':r.status+' '+r.statusText);
    return r.json()
  })
  .then(function(d){
    var el=document.createElement('div');
    el.textContent='Imported to Mental Metal ';
    var a=document.createElement('a');
    a.href=u+'/capture/'+d.id;
    a.target='_blank';
    a.textContent='View capture';
    a.style.cssText='color:#fff;text-decoration:underline;margin-left:8px';
    el.appendChild(a);
    w(el,'#22c55e',4000)
  })
  .catch(function(e){
    var el=document.createElement('div');
    el.textContent='Import failed: '+e.message;
    w(el,'#ef4444',6000)
  });
  function w(content,bg,dur){
    var d=document.createElement('div');
    d.appendChild(content);
    d.style.cssText='position:fixed;top:16px;left:50%;transform:translateX(-50%);z-index:999999;padding:12px 20px;border-radius:8px;font-family:system-ui,sans-serif;font-size:14px;color:#fff;background:'+bg+';box-shadow:0 4px 12px rgba(0,0,0,.3);cursor:pointer;max-width:500px;text-align:center';
    d.onclick=function(){d.remove()};
    document.body.appendChild(d);
    setTimeout(function(){if(d.parentNode)d.remove()},dur)
  }
})()
  `.trim();

  // Replace placeholders using function replacers to avoid $-injection
  const filled = source
    .replace('__INSTANCE_URL__', () => instanceUrl.replace(/'/g, "\\'"))
    .replace('__PAT__', () => pat.replace(/'/g, "\\'"));

  // Minify: collapse whitespace and remove newlines
  const minified = filled
    .replace(/\s*\n\s*/g, '')
    .replace(/\s{2,}/g, ' ');

  return 'javascript:' + encodeURIComponent(minified);
}
