import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Api, describeError } from '../core/api';
import { DocumentDto } from '../core/models';

const POLL_INTERVAL_MS = 3000;

/** Shared state for the document list, plus which documents the questions are restricted to. */
@Injectable({ providedIn: 'root' })
export class DocumentsStore {
  private readonly api = inject(Api);
  private pollTimer: ReturnType<typeof setTimeout> | undefined;

  readonly documents = signal<DocumentDto[]>([]);
  readonly loading = signal(false);
  readonly uploading = signal(0);
  readonly error = signal<string | null>(null);

  /** Empty selection = ask about every Ready document. */
  readonly selectedIds = signal<ReadonlySet<string>>(new Set());

  readonly readyDocuments = computed(() => this.documents().filter(d => d.status === 'Ready'));
  readonly selectedDocuments = computed(() =>
    this.readyDocuments().filter(d => this.selectedIds().has(d.id)),
  );

  async refresh(): Promise<void> {
    clearTimeout(this.pollTimer);
    this.loading.set(true);

    try {
      const documents = await firstValueFrom(this.api.listDocuments());
      this.documents.set(documents);
      this.error.set(null);
      this.dropStaleSelection(documents);

      // Keep refreshing while something is still being processed in the background.
      if (documents.some(d => d.status === 'Processing')) {
        this.pollTimer = setTimeout(() => this.refresh(), POLL_INTERVAL_MS);
      }
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  async upload(files: File[]): Promise<void> {
    const pdfs = files.filter(f => f.name.toLowerCase().endsWith('.pdf'));
    if (pdfs.length < files.length) {
      this.error.set('Só arquivos PDF são aceitos.');
    }

    for (const file of pdfs) {
      this.uploading.update(n => n + 1);
      try {
        await firstValueFrom(this.api.uploadDocument(file));
      } catch (error) {
        this.error.set(`${file.name}: ${describeError(error)}`);
      } finally {
        this.uploading.update(n => n - 1);
      }
    }

    await this.refresh();
  }

  async remove(document: DocumentDto): Promise<void> {
    try {
      await firstValueFrom(this.api.deleteDocument(document.id));
    } catch (error) {
      this.error.set(describeError(error));
    }

    await this.refresh();
  }

  toggleSelection(id: string): void {
    this.selectedIds.update(current => {
      const next = new Set(current);
      if (!next.delete(id)) {
        next.add(id);
      }
      return next;
    });
  }

  clearSelection(): void {
    this.selectedIds.set(new Set());
  }

  private dropStaleSelection(documents: DocumentDto[]): void {
    const ready = new Set(documents.filter(d => d.status === 'Ready').map(d => d.id));
    const current = this.selectedIds();
    if ([...current].some(id => !ready.has(id))) {
      this.selectedIds.set(new Set([...current].filter(id => ready.has(id))));
    }
  }
}
