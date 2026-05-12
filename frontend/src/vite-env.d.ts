/// <reference types="vite/client" />

interface ImportMetaEnv {
    /** Optional — if set, footer shows a mailto link. */
    readonly VITE_CONTACT_EMAIL?: string;
}

interface ImportMeta {
    readonly env: ImportMetaEnv;
}
