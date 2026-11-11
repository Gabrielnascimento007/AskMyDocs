import { Component, ElementRef, effect, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DocumentsStore } from '../documents/documents-store';
import { AskStore, Turn } from './ask-store';

@Component({
  selector: 'app-ask-panel',
  imports: [FormsModule],
  templateUrl: './ask-panel.html',
  styleUrl: './ask-panel.scss',
})
export class AskPanel {
  protected readonly store = inject(AskStore);
  protected readonly documents = inject(DocumentsStore);
  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');

  protected readonly question = signal('');
  protected readonly copiedTurn = signal<number | null>(null);

  protected readonly suggestions = [
    'Sobre o que é este documento?',
    'Faça um resumo dos pontos principais.',
    'Quais datas e prazos aparecem?',
  ];

  constructor() {
    // Follow the conversation as questions and answers arrive.
    effect(() => {
      this.store.turns();
      setTimeout(() => {
        const element = this.scroller()?.nativeElement;
        element?.scrollTo({ top: element.scrollHeight, behavior: 'smooth' });
      });
    });
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.submit();
    }
  }

  protected submit(text = this.question()): void {
    if (!text.trim() || this.store.busy()) {
      return;
    }
    this.question.set('');
    this.store.ask(text);
  }

  protected isOpen(turn: Turn, index: number): boolean {
    const open = this.store.openSource();
    return open?.turnId === turn.id && open.index === index;
  }

  protected summary(turn: Turn): string {
    const sources = turn.sources.length;
    const files = new Set(turn.sources.map(s => s.documentId)).size;
    return `${sources} fonte${sources === 1 ? '' : 's'} · ${files} documento${files === 1 ? '' : 's'}`;
  }

  protected async copy(turn: Turn): Promise<void> {
    try {
      await navigator.clipboard.writeText(turn.answer);
      this.copiedTurn.set(turn.id);
      setTimeout(() => this.copiedTurn.set(null), 2000);
    } catch {
      // Clipboard can be blocked (e.g. http on another host); nothing else to do.
    }
  }
}
