import { Component, computed, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { HttpErrorService } from './core/http/http-error.service';
import { SidebarComponent } from './core/layout/sidebar/sidebar.component';
import { HeaderComponent } from './core/layout/header/header.component';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  imports: [RouterOutlet, SidebarComponent, HeaderComponent],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly httpErrors = inject(HttpErrorService);
  private readonly router = inject(Router);

  // Convert navigation events to signal for reactive template
  private readonly navigationEnd = toSignal(this.router.events);
  protected readonly isLoginPage = computed(() => {
    this.navigationEnd();
    const url = this.router.url;
    return url.split(/[?#]/, 1)[0] === '/login';
  });
}
