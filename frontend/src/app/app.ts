import { Component, signal, Inject, PLATFORM_ID, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { isPlatformBrowser } from '@angular/common';
import { LoaderComponent } from './shared/components/loader/loader';
import { environment } from '../environments/environment';
import { VersionShieldingService } from './services/version-shielding.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, LoaderComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('asxcr');
  private versionShielding = inject(VersionShieldingService);

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    if (isPlatformBrowser(this.platformId)) {
      this.updateFavicon();
      this.initializeVersionShielding();
    }
  }

  private initializeVersionShielding(): void {
    setTimeout(() => {
      this.versionShielding.initializeShielding();
    }, 100);
  }

  private updateFavicon(): void {
    const baseUrl = environment.baseUrl;
    const faviconPath = `${baseUrl}/assets/images/tp-logo.svg`;
    
    // Update existing links
    const links = document.querySelectorAll("link[rel*='icon']");
    links.forEach((link: any) => {
      link.href = faviconPath;
    });

    // Fallback if no link found
    if (links.length === 0) {
      const link = document.createElement('link');
      link.rel = 'icon';
      link.type = 'image/svg+xml';
      link.href = faviconPath;
      document.getElementsByTagName('head')[0].appendChild(link);
    }
  }
}
