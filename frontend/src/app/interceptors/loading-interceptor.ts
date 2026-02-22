import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoadingService } from '../services/loading';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);

  const skipGlobalLoader =
    req.headers.has('X-Skip-Loading') ||
    req.url.includes('User/SearchADUsers') ||
    req.url.includes('User/GetADUserPhoto') ||
    req.url.includes('EmailService/GetTPAssist');

  if (!skipGlobalLoader) {
    loadingService.show();
  }

  return next(req).pipe(
    finalize(() => {
      if (!skipGlobalLoader) {
        loadingService.hide();
      }
    })
  );
};
