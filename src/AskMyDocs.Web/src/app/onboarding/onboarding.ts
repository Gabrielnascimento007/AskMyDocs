import { Component, inject, signal } from '@angular/core';
import { DocumentsStore } from '../documents/documents-store';

/** First visit: no documents yet. */
@Component({
  selector: 'app-onboarding',
  templateUrl: './onboarding.html',
  styleUrl: './onboarding.scss',
})
export class Onboarding {
  protected readonly store = inject(DocumentsStore);
  protected readonly dragging = signal(false);

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
}
