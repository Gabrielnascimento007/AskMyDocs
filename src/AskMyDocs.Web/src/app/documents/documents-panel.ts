import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { DocumentDto, DocumentStatus } from '../core/models';
import { DocumentsStore } from './documents-store';

@Component({
  selector: 'app-documents-panel',
  imports: [DatePipe],
  templateUrl: './documents-panel.html',
  styleUrl: './documents-panel.scss',
})
export class DocumentsPanel implements OnInit {
  protected readonly store = inject(DocumentsStore);
  protected readonly dragging = signal(false);

  protected readonly statusLabels: Record<DocumentStatus, string> = {
    Processing: 'Processando',
    Ready: 'Pronto',
    Failed: 'Falhou',
  };

  ngOnInit(): void {
    this.store.refresh();
  }

  protected onFilesPicked(input: HTMLInputElement): void {
    this.store.upload(Array.from(input.files ?? []));
    input.value = '';
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    this.store.upload(Array.from(event.dataTransfer?.files ?? []));
  }

  protected confirmRemove(document: DocumentDto): void {
    if (confirm(`Remover "${document.fileName}"? Os trechos indexados também serão apagados.`)) {
      this.store.remove(document);
    }
  }
}
