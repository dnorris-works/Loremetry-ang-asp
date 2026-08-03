export type FieldType = 'Text' | 'Number' | 'Boolean' | 'Date' | 'Json';

export const FIELD_TYPES: FieldType[] = ['Text', 'Number', 'Boolean', 'Date', 'Json'];

export interface CollectionSummary {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  fieldCount: number;
  entryCount: number;
  updatedAt: string;
}

export interface CollectionField {
  id: string;
  name: string;
  fieldKey: string;
  fieldType: FieldType;
  isRequired: boolean;
  displayOrder: number;
}

export interface CollectionDetail {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  createdAt: string;
  updatedAt: string;
  fields: CollectionField[];
}

export interface CollectionEntry {
  id: string;
  collectionId: string;
  values: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
}

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

export interface CreateCollectionRequest {
  name: string;
  slug: string;
  description?: string | null;
}

export interface CreateFieldRequest {
  name: string;
  fieldKey: string;
  fieldType: FieldType;
  isRequired: boolean;
  displayOrder: number;
}
