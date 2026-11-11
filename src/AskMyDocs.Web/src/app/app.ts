import { Component } from '@angular/core';
import { AskPanel } from './ask/ask-panel';
import { DocumentsPanel } from './documents/documents-panel';

@Component({
  selector: 'app-root',
  imports: [DocumentsPanel, AskPanel],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
