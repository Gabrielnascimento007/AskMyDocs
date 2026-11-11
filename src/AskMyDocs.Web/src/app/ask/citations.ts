export type AnswerSegment =
  | { kind: 'text'; text: string; bold: boolean }
  | { kind: 'citation'; index: number };

export interface AnswerBlock {
  kind: 'paragraph' | 'item';
  segments: AnswerSegment[];
}

/**
 * Turns the model's answer into blocks the template can render without innerHTML:
 * "* item" / "- item" / "1. item" lines become list items, **text** becomes bold,
 * and [n] becomes a clickable citation.
 */
export function formatAnswer(answer: string): AnswerBlock[] {
  const blocks: AnswerBlock[] = [];
  let paragraph: string[] = [];

  const flushParagraph = () => {
    if (paragraph.length > 0) {
      blocks.push({ kind: 'paragraph', segments: parseInline(paragraph.join('\n')) });
      paragraph = [];
    }
  };

  for (const rawLine of answer.split(/\r?\n/)) {
    const line = rawLine.trim();
    const item = /^(?:[*\-•]|\d+[.)])\s+(.*)$/.exec(line);

    if (item) {
      flushParagraph();
      blocks.push({ kind: 'item', segments: parseInline(item[1]) });
    } else if (line.length === 0) {
      flushParagraph();
    } else {
      paragraph.push(line);
    }
  }

  flushParagraph();
  return blocks;
}

function parseInline(text: string): AnswerSegment[] {
  const segments: AnswerSegment[] = [];

  // Odd parts sit between ** markers, i.e. are bold.
  text.split('**').forEach((part, i) => {
    const bold = i % 2 === 1;
    let last = 0;

    for (const match of part.matchAll(/\[(\d+)\]/g)) {
      if (match.index > last) {
        segments.push({ kind: 'text', text: part.slice(last, match.index), bold });
      }
      segments.push({ kind: 'citation', index: Number(match[1]) });
      last = match.index + match[0].length;
    }

    if (last < part.length) {
      segments.push({ kind: 'text', text: part.slice(last), bold });
    }
  });

  return segments;
}
