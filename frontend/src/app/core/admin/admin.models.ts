export interface SchemaInfo {
  name: string;
}

export interface SchemaObject {
  name: string;
  type: string;
}

export interface ColumnInfo {
  name: string;
  dataType: string;
  isNullable: boolean;
  defaultValue: string | null;
}

export interface SqlQueryResult {
  columns: string[];
  rows: unknown[][];
  rowsAffected: number | null;
  message: string | null;
}
