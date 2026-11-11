import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Api, describeError } from '../core/api';
import { AskSource } from '../core/models';
import { DocumentsStore } from '../documents/documents-store';
import { AnswerBlock, formatAnswer } from './citations';

export interface Turn {
  id: number;
  question: string;
  scope: string[];
  status: 'pending' | 'done' | 'error';
  answer: string;
  blocks: AnswerBlock[];
  sources: AskSource[];
  error?: string;
}

export interface OpenSource {
  turnId: number;
  index: number;
}

/** The conversation and which cited source is open in the source panel. */
@Injectable({ providedIn: 'root' })
export class AskStore {
  private readonly api = inject(Api);
  private readonly documents = inject(DocumentsStore);
  private nextId = 1;

  readonly turns = signal<Turn[]>([]);
  readonly busy = signal(false);
  readonly openSource = signal<OpenSource | null>(null);

  /** The open source together with its turn, for the source panel. */
  readonly openSourceDetails = computed(() => {
    const open = this.openSource();
    const turn = open && this.turns().find(t => t.id === open.turnId);
    const source = turn?.sources.find(s => s.index === open!.index);
    return turn && source ? { turn, source } : null;
  });

  async ask(question: string): Promise<void> {
    question = question.trim();
    if (!question || this.busy()) {
      return;
    }

    const selected = this.documents.selectedDocuments();
    const turn: Turn = {
      id: this.nextId++,
      question,
      scope: selected.map(d => d.fileName),
      status: 'pending',
      answer: '',
      blocks: [],
      sources: [],
    };

    this.turns.update(turns => [...turns, turn]);
    this.busy.set(true);

    try {
      const response = await firstValueFrom(
        this.api.ask({
          question,
          documentIds: selected.length > 0 ? selected.map(d => d.id) : undefined,
        }),
      );
      this.update(turn.id, {
        status: 'done',
        answer: response.answer,
        blocks: formatAnswer(response.answer),
        sources: response.sources,
      });
    } catch (error) {
      this.update(turn.id, { status: 'error', error: describeError(error) });
    } finally {
      this.busy.set(false);
    }
  }

  showSource(turnId: number, index: number): void {
    this.openSource.set({ turnId, index });
  }

  /** Moves to the previous (-1) or next (+1) source of the open answer. */
  stepSource(direction: -1 | 1): void {
    const details = this.openSourceDetails();
    if (!details) {
      return;
    }

    const sources = details.turn.sources;
    const position = sources.findIndex(s => s.index === details.source.index);
    const next = sources[position + direction];
    if (next) {
      this.showSource(details.turn.id, next.index);
    }
  }

  closeSource(): void {
    this.openSource.set(null);
  }

  clear(): void {
    this.turns.set([]);
    this.openSource.set(null);
  }

  private update(id: number, changes: Partial<Turn>): void {
    this.turns.update(turns => turns.map(t => (t.id === id ? { ...t, ...changes } : t)));
  }
}
