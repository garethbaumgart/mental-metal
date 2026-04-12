export interface AiProviderStatus {
  isConfigured: boolean;
  provider: string | null;
  model: string | null;
  maxTokens: number | null;
  tasteBudget: TasteBudget;
}

export interface TasteBudget {
  remaining: number;
  dailyLimit: number;
  isEnabled: boolean;
}

export interface ConfigureAiProviderRequest {
  provider: string;
  apiKey?: string;
  model: string;
  maxTokens?: number | null;
}

export interface ValidateAiProviderRequest {
  provider: string;
  apiKey: string;
  model: string;
}

export interface ValidateAiProviderResponse {
  success: boolean;
  modelName: string | null;
  error: string | null;
}

export interface AiModelInfo {
  id: string;
  name: string;
  isDefault: boolean;
}

export interface AvailableModelsResponse {
  provider: string;
  models: AiModelInfo[];
}

export type AiProviderType = 'Anthropic' | 'OpenAI' | 'Google';

export interface ProviderOption {
  name: AiProviderType;
  label: string;
  icon: string;
  keyUrl: string;
  keyUrlLabel: string;
}

export const AI_PROVIDERS: ProviderOption[] = [
  {
    name: 'Anthropic',
    label: 'Anthropic',
    icon: 'pi pi-bolt',
    keyUrl: 'https://console.anthropic.com/settings/keys',
    keyUrlLabel: 'Open Anthropic Console',
  },
  {
    name: 'OpenAI',
    label: 'OpenAI',
    icon: 'pi pi-microchip-ai',
    keyUrl: 'https://platform.openai.com/api-keys',
    keyUrlLabel: 'Open OpenAI Platform',
  },
  {
    name: 'Google',
    label: 'Google',
    icon: 'pi pi-google',
    keyUrl: 'https://aistudio.google.com/apikey',
    keyUrlLabel: 'Open AI Studio',
  },
];
