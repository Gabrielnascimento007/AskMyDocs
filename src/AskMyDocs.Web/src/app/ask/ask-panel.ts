import { DecimalPipe } from '@angular/common';
import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { Api, describeError } from '../core/api';
import { AskSource } from '../core/models';
import { DocumentsStore } from '../documents/documents-store';
import { AnswerBlock, formatAnswer } from './citations';

interface Turn {
  id: number;
  question: string;
  scope: string[];
  status: 'pending' | 'done' | 'error';
  blocks: AnswerBlock[];
  sources: AskSource[];
  error?: string;
  highlighted?: number;
}

@Component({
  selector: 'app-ask-panel',
  imports: [FormsModule, DecimalPipe],
  templateUrl: './ask-panel.html',
  styleUrl: './ask-panel.scss',
})
export class AskPanel {
  private readonly api = inject(Api);
  protected readonly documents = inject(DocumentsStore);
  private readonly conversation = viewChild<ElementRef<HTMLElement>>('conversation');

  protected readonly question = signal('');
  protected readonly turns = signal<Turn[]>([]);
  protected readonly busy = signal(false);
  private nextId = 1;

  protected readonly suggestions = [
    'Sobre o que é este documento?',
    'Faça um resumo dos pontos principais.',
    'Quais datas e prazos aparecem?',
  ];

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.submit();
    }
  }

  protected useSuggestion(text: string): void {
    this.question.set(text);
    this.submit();
  }

  protected async submit(): Promise<void> {
    const question = this.question().trim();
    if (!question || this.busy()) {
      return;
    }

    const selected = this.documents.selectedDocuments();
    const turn: Turn = {
      id: this.nextId++,
      question,
      scope: selected.map(d => d.fileName),
      status: 'pending',
      blocks: [],
      sources: [],
    };

    this.turns.update(turns => [...turns, turn]);
    this.question.set('');
    this.busy.set(true);
    this.scrollToBottom();

    try {
      const response = await firstValueFrom(
        this.api.ask({
          question,
          documentIds: selected.length > 0 ? selected.map(d => d.id) : undefined,
        }),
      );
      this.updateTurn(turn.id, {
        status: 'done',
        blocks: formatAnswer(response.answer),
        sources: response.sources,
      });
    } catch (error) {
      this.updateTurn(turn.id, { status: 'error', error: describeError(error) });
    } finally {
      this.busy.set(false);
      this.scrollToBottom();
    }
  }

  protected showSource(turn: Turn, index: number): void {
    this.updateTurn(turn.id, { highlighted: index });
    document
      .getElementById(this.sourceElementId(turn, index))
      ?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  }

  protected sourceElementId(turn: Turn, index: number): string {
    return `turn-${turn.id}-source-${index}`;
  }

  protected clear(): void {
    this.turns.set([]);
  }

  private updateTurn(id: number, changes: Partial<Turn>): void {
    this.turns.update(turns => turns.map(t => (t.id === id ? { ...t, ...changes } : t)));
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const element = this.conversation()?.nativeElement;
      element?.scrollTo({ top: element.scrollHeight, behavior: 'smooth' });
    });
  }
}
