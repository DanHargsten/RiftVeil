/** Riot Games — Legal Jibber Jabber (fan project notice). */
const RIOT_NOTICE_TAIL =
    ` was created under Riot Games' "Legal Jibber Jabber" policy using assets owned by Riot Games. Riot Games does not endorse or sponsor this project.`;

function contactEmailFromEnv(): string | undefined {
    const raw = import.meta.env.VITE_CONTACT_EMAIL;
    if (typeof raw !== "string") return undefined;
    const trimmed = raw.trim();
    return trimmed.includes("@") ? trimmed : undefined;
}

export function SiteFooter() {
    const contactEmail = contactEmailFromEnv();

    return (
        <footer className="site-footer" role="contentinfo">
            <div className="site-footer__inner">
                <div className="site-footer__body">
                    <p className="site-footer__text" lang="en">
                        <strong>Rift Veil</strong>
                        {RIOT_NOTICE_TAIL}
                    </p>
                    {contactEmail !== undefined && (
                        <p className="site-footer__contact">
                            <a
                                className="site-footer__mailto"
                                href={`mailto:${contactEmail}`}
                                aria-label="Send email (opens your mail app)"
                            >
                                Contact
                            </a>
                        </p>
                    )}
                </div>
            </div>
        </footer>
    );
}
