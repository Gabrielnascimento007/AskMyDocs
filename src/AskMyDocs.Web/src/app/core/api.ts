import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AskRequest, AskResponse, DocumentDto } from './models';

/** Thin wrapper over the AskMyDocs API (proxied by the dev server, see proxy.conf.json). */
@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);

  listDocuments(): Observable<DocumentDto[]> {
    return this.http.get<DocumentDto[]>('/documents');
  }

  uploadDocument(file: File): Observable<DocumentDto> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<DocumentDto>('/documents', form);
  }

  deleteDocument(id: string): Observable<void> {
    return this.http.delete<void>(`/documents/${id}`);
  }

  ask(request: AskRequest): Observable<AskResponse> {
    return this.http.post<AskResponse>('/ask', request);
  }
}

/** Turns an HTTP error (usually Problem Details from the API) into a readable message. */
export function describeError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Ocorreu um erro inesperado.';
  }

  if (error.status === 0 || error.status === 502 || error.status === 504) {
    return 'Não foi possível conectar à API. Confira se ela está rodando no Visual Studio.';
  }

  const problem = error.error as
    | { title?: string; detail?: string; errors?: Record<string, string[]> }
    | null;

  const validation = problem?.errors ? Object.values(problem.errors).flat() : [];
  if (validation.length > 0) {
    return validation.join(' ');
  }

  return problem?.detail ?? problem?.title ?? `Erro ${error.status} ao chamar a API.`;
}
