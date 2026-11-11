import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AskPanel } from './ask/ask-panel';
import { AskStore } from './ask/ask-store';
import { SourcePanel } from './ask/source-panel';
import { DocumentsPanel } from './documents/documents-panel';
import { DocumentsStore } from './documents/documents-store';
import { Onboarding } from './onboarding/onboarding';

@Component({
  selector: 'app-root',
  imports: [DocumentsPanel, AskPanel, SourcePanel, Onboarding],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected readonly documents = inject(DocumentsStore);
  protected readonly ask = inject(AskStore);

  /** Phone layout: the library slides in from the side. */
  protected readonly libraryOpen = signal(false);

  /** First visit: nothing uploaded yet, so show the onboarding screen instead of the workspace. */
  protected readonly showOnboarding = computed(
    () =>
      this.documents.loaded() &&
      this.documents.connected() &&
      this.documents.documents().length === 0 &&
      this.documents.uploading() === 0,
  );

  protected readonly scopeLabel = computed(() => {
    const selected = this.documents.selectedIds().size;
    const ready = this.documents.readyDocuments().length;
    if (selected > 0) {
      return `${selected} documento${selected > 1 ? 's' : ''} selecionado${selected > 1 ? 's' : ''}`;
    }
    return `${ready} documento${ready === 1 ? '' : 's'} pronto${ready === 1 ? '' : 's'}`;
  });

  ngOnInit(): void {
    this.documents.refresh();
  }
}
