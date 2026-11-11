export type DocumentStatus = 'Processing' | 'Ready' | 'Failed';

export interface DocumentDto {
  id: string;
  fileName: string;
  status: DocumentStatus;
  errorMessage: string | null;
  chunkCount: number;
  createdAt: string;
}

export interface AskRequest {
  question: string;
  documentIds?: string[];
  topK?: number;
}

export interface AskSource {
  index: number;
  documentId: string;
  fileName: string;
  pageNumber: number;
  excerpt: string;
  similarity: number;
}

export interface AskResponse {
  answer: string;
  sources: AskSource[];
}
