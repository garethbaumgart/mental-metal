import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './shared/components/sidebar.component';
import { GlobalChatLauncherComponent } from './shared/components/global-chat-launcher.component';
import { GlobalChatSlideOverComponent } from './shared/components/global-chat-slide-over.component';

@Component({
  selector: 'app-root',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, SidebarComponent, GlobalChatLauncherComponent, GlobalChatSlideOverComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly sidebarOpen = signal(false);
}
