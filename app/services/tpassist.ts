import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, Subject, debounceTime, distinctUntilChanged, switchMap, catchError } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

// Models for TP Assist Search
export interface TPAssistSearchResponse {
  success: boolean;
  route?: string;
  label?: string;
  message?: string;
  isHelpResponse?: boolean;
  confidence?: number;
  category?: string;
  suggestions: TPAssistSuggestion[];
}

export interface TPAssistSuggestion {
  label: string;
  route?: string;
  description?: string;
  type?: 'navigate' | 'help';
  answerId?: number;
  confidence?: number;
  icon?: string;
}

@Injectable({ providedIn: 'root' })
export class TPAssistService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;
  
  private searchSubject = new Subject<string>();
  
  public searchResults$ = this.searchSubject.pipe(
    debounceTime(200), // Fast response
    distinctUntilChanged(),
    switchMap(query => {
      if (!query || query.trim().length < 2) {
        return of(null);
      }
      return this.search(query).pipe(catchError(() => of(null)));
    })
  );

  triggerSearch(query: string): void {
    this.searchSubject.next(query);
  }

  search(query: string, answerId?: number): Observable<ApiResponse<TPAssistSearchResponse>> {
    return this.http.post<ApiResponse<TPAssistSearchResponse>>(
      `${this.apiUrl}TPAssist/Search`,
      { query, answerId }
    );
  }

  getAnswer(answerId: number): Observable<ApiResponse<TPAssistSearchResponse>> {
    return this.search('', answerId);
  }

  getSuggestions(): Observable<ApiResponse<TPAssistSuggestion[]>> {
    return this.http.get<ApiResponse<TPAssistSuggestion[]>>(`${this.apiUrl}TPAssist/GetSuggestions`);
  }
}
