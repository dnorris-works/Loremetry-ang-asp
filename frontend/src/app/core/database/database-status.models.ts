export type DbConnectionStatus = 'connected' | 'disconnected' | 'checking';

export interface DatabaseHealthResponse {
  status: string;
  database: string;
}
