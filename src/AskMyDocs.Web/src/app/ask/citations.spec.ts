import { formatAnswer } from './citations';

describe('formatAnswer', () => {
  it('turns [n] into citation segments', () => {
    const [block] = formatAnswer('São 30 dias [2] por ano.');

    expect(block.segments).toEqual([
      { kind: 'text', text: 'São 30 dias ', bold: false },
      { kind: 'citation', index: 2 },
      { kind: 'text', text: ' por ano.', bold: false },
    ]);
  });

  it('turns bullet lines into list items and **text** into bold', () => {
    const blocks = formatAnswer('Com base nos documentos:\n\n* **Férias:** 30 dias [2].\n- Remoto [1][3]');

    expect(blocks.map(b => b.kind)).toEqual(['paragraph', 'item', 'item']);
    expect(blocks[1].segments[0]).toEqual({ kind: 'text', text: 'Férias:', bold: true });
    expect(blocks[2].segments.filter(s => s.kind === 'citation')).toEqual([
      { kind: 'citation', index: 1 },
      { kind: 'citation', index: 3 },
    ]);
  });

  it('keeps consecutive lines in the same paragraph and splits on blank lines', () => {
    const blocks = formatAnswer('linha 1\nlinha 2\n\nlinha 3');

    expect(blocks).toHaveLength(2);
    expect(blocks[0].segments[0]).toEqual({ kind: 'text', text: 'linha 1\nlinha 2', bold: false });
  });
});
