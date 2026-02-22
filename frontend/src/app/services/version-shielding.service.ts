import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class VersionShieldingService {
  
  private readonly commentsToRemove = [
    'Angular',
    'ng-',
    'Zone.js',
    'bootstrap',
    'jQuery'
  ];

  initializeShielding(): void {
    if (typeof window === 'undefined' || typeof document === 'undefined') {
      return;
    }

    this.removeAngularVersionAttribute();
    this.maskMetaTags();
    this.removeComments();
    this.obfuscateGlobalVariables();
    this.maskBootstrapSignatures();
    this.maskChartJsSignatures();
    
    this.setupMutationObserver();
  }

  private removeAngularVersionAttribute(): void {
    const appRoot = document.querySelector('app-root');
    if (appRoot) {
      appRoot.removeAttribute('ng-version');
    }

    document.querySelectorAll('[ng-version]').forEach(el => {
      el.removeAttribute('ng-version');
    });
  }

  private maskMetaTags(): void {
    const generator = document.querySelector('meta[name="generator"]');
    if (generator) {
      generator.remove();
    }

    const framework = document.querySelector('meta[name="framework"]');
    if (framework) {
      framework.remove();
    }
  }

  private removeComments(): void {
    const iterator = document.createNodeIterator(
      document.documentElement,
      NodeFilter.SHOW_COMMENT
    );

    const comments: Node[] = [];
    let currentNode: Node | null;

    while (currentNode = iterator.nextNode()) {
      const commentText = currentNode.textContent?.toLowerCase() || '';
      const shouldRemove = this.commentsToRemove.some(keyword => 
        commentText.includes(keyword.toLowerCase())
      );
      
      if (shouldRemove) {
        comments.push(currentNode);
      }
    }

    comments.forEach(comment => comment.parentNode?.removeChild(comment));
  }

  private obfuscateGlobalVariables(): void {
    if (typeof window !== 'undefined') {
      const win = window as any;
      
      // Remove Angular debug helpers
      if (win.ng) {
        delete win.ng;
      }

      if (win.getAllAngularRootElements) {
        delete win.getAllAngularRootElements;
      }

      if (win.getAngularTestability) {
        delete win.getAngularTestability;
      }

      // Hide version properties only - don't delete the objects themselves
      if (win.Zone?.version) {
        try {
          delete win.Zone.version;
        } catch {}
      }

      if (win.$?.fn?.jquery) {
        try {
          delete win.$.fn.jquery;
        } catch {}
      }

      if (win.jQuery?.fn?.jquery) {
        try {
          delete win.jQuery.fn.jquery;
        } catch {}
      }

      if (win.Chart?.version) {
        try {
          delete win.Chart.version;
        } catch {}
      }

      if (win.Swal?.version) {
        try {
          delete win.Swal.version;
        } catch {}
      }

      if (win.swal?.version) {
        try {
          delete win.swal.version;
        } catch {}
      }
    }
  }

  private maskBootstrapSignatures(): void {
    const win = window as any;
    
    // Completely remove Bootstrap global object to prevent detection
    if (win.bootstrap) {
      try {
        delete win.bootstrap;
      } catch {}
    }

    // Override to prevent re-detection
    try {
      Object.defineProperty(win, 'bootstrap', {
        get: () => undefined,
        set: () => {},
        configurable: false,
        enumerable: false
      });
    } catch {}

    // Remove Bootstrap-specific data attributes that Wappalyzer checks
    document.querySelectorAll('[data-bs-version], [data-bootstrap-version], [data-bs-toggle], [data-bs-target]').forEach(el => {
      el.removeAttribute('data-bs-version');
      el.removeAttribute('data-bootstrap-version');
      // Keep functionality attributes but store them temporarily
      const bsToggle = el.getAttribute('data-bs-toggle');
      const bsTarget = el.getAttribute('data-bs-target');
      if (bsToggle) {
        el.setAttribute('data-toggle', bsToggle);
        el.removeAttribute('data-bs-toggle');
      }
      if (bsTarget) {
        el.setAttribute('data-target', bsTarget);
        el.removeAttribute('data-bs-target');
      }
    });

    // Remove meta tags mentioning Bootstrap
    document.querySelectorAll('meta[name*="bootstrap"], meta[content*="bootstrap"]').forEach(meta => {
      const name = meta.getAttribute('name');
      const content = meta.getAttribute('content');
      if ((name && /bootstrap/i.test(name)) || (content && /bootstrap.*\d+\.\d+/i.test(content))) {
        meta.remove();
      }
    });

    // Modify stylesheet hrefs to remove version numbers
    document.querySelectorAll('link[rel="stylesheet"]').forEach(link => {
      const href = link.getAttribute('href');
      if (href && /bootstrap[.-]?\d+\.\d+/i.test(href)) {
        const cleanHref = href.replace(/bootstrap[.-]?\d+\.\d+\.\d+/gi, 'bootstrap');
        link.setAttribute('href', cleanHref);
      }
    });
  }

  private maskChartJsSignatures(): void {
    const win = window as any;
    
    if (win.Chart) {
      try {
        // Remove all version indicators
        delete win.Chart.version;
        delete win.Chart.VERSION;
        
        // Remove defaults that might contain version info
        if (win.Chart.defaults) {
          delete win.Chart.defaults.global?.version;
        }
        
        // Mask Chart.js constructor properties
        if (win.Chart.Chart?.version) delete win.Chart.Chart.version;
        if (win.Chart.Chart?.VERSION) delete win.Chart.Chart.VERSION;
        
        // Override version property to return undefined
        Object.defineProperty(win.Chart, 'version', {
          get: () => undefined,
          configurable: true,
          enumerable: false
        });
        
        Object.defineProperty(win.Chart, 'VERSION', {
          get: () => undefined,
          configurable: true,
          enumerable: false
        });
      } catch {}
    }

    // Mask Chart.js script src
    document.querySelectorAll('script[src*="chart"]').forEach(script => {
      const src = script.getAttribute('src');
      if (src && /chart[.-]\d+\.\d+\.\d+/i.test(src)) {
        const newSrc = src.replace(/chart[.-]\d+\.\d+\.\d+/gi, 'chart');
        script.setAttribute('src', newSrc);
      }
    });
  }

  private setupMutationObserver(): void {
    if (typeof MutationObserver === 'undefined') {
      return;
    }

    const observer = new MutationObserver(mutations => {
      mutations.forEach(mutation => {
        if (mutation.type === 'attributes') {
          const target = mutation.target as Element;
          const attrName = mutation.attributeName;
          
          if (attrName === 'ng-version') {
            target.removeAttribute(attrName);
          }
        }
      });
    });

    observer.observe(document.documentElement, {
      attributes: true,
      subtree: true,
      attributeFilter: ['ng-version']
    });
  }
}
