export interface Environment {
    readonly production: boolean;
    readonly apiBaseUrl: string;
    readonly allowDevBypass: boolean;
}

export const environment: Environment = {
    production: false,
    apiBaseUrl: 'https://electricai.ro',
    allowDevBypass: true,
};
