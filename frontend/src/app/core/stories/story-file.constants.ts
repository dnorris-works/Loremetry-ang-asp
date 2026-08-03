export const STORY_FILE_EXTENSIONS = ['.md', '.txt', '.docx'] as const;

export const STORY_FILE_ACCEPT = [
  '.md',
  '.txt',
  '.docx',
  'text/markdown',
  'text/plain',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
].join(',');

export const STORY_FILE_PICKER_TYPES = [
  {
    description: 'Story files',
    accept: {
      'text/markdown': ['.md'],
      'text/plain': ['.txt'],
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx'],
    },
  },
];
