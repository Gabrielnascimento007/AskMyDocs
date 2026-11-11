import { DecimalPipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { AskStore } from './ask-store';

/** The cited excerpt opened from a citation: side panel on desktop, bottom sheet on phones. */
@Component({
  selector: 'app-source-panel',
  imports: [DecimalPipe],
  templateUrl: './source-panel.html',
  styleUrl: './source-panel.scss',
  host: { '(document:keydown.escape)': 'store.closeSource()' },
})
export class SourcePanel {
  protected readonly store = inject(AskStore);
  protected readonly details = this.store.openSourceDetails;

  protected readonly others = computed(() => {
    const details = this.details();
    return details ? details.turn.sources.filter(s => s.index !== details.source.index) : [];
  });

  protected readonly position = computed(() => {
    const details = this.details();
    if (!details) {
      return { first: true, last: true };
    }
    const sources = details.turn.sources;
    const i = sources.findIndex(s => s.index === details.source.index);
    return { first: i <= 0, last: i >= sources.length - 1 };
  });
}
