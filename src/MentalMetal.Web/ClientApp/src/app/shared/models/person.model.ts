export type PersonType = 'DirectReport' | 'Peer' | 'Stakeholder' | 'Candidate' | 'External';

export interface Person {
  id: string;
  userId: string;
  name: string;
  type: PersonType;
  email: string | null;
  role: string | null;
  team: string | null;
  notes: string | null;
  aliases: string[];
  isArchived: boolean;
  archivedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePersonRequest {
  name: string;
  type: PersonType;
  email?: string;
  role?: string;
  team?: string;
  aliases?: string[];
}

export interface UpdatePersonRequest {
  name: string;
  email: string | null;
  role: string | null;
  team: string | null;
  notes: string | null;
  aliases?: string[];
}
