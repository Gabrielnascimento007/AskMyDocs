import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { DocumentDto } from '../core/models';
import { DocumentsStore } from './documents-store';

@Component({
  selector: 'app-documents-panel',
  imports: [DatePipe],
  templateUrl: './documents-panel.html',
  styleUrl: './documents-panel.scss',
})
export class DocumentsPanel {
  protected readonly store = inject(DocumentsStore);

  protected onFilesPicked(input: HTMLInputElement): void {
    this.store.upload(Array.from(input.files ?? []));
    input.value = '';
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.store.upload(Array.from(event.dataTransfer?.files ?? []));
  }

  protected confirmRemove(document: DocumentDto): void {
    if (confirm(`Remover "${document.fileName}"? Os trechos indexados também serão apagados.`)) {
      this.store.remove(document);
    }
  }
}
