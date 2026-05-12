import { MatchList } from "@/components/MatchList";
import { useSpoilerPrefs } from "@/hooks/useSpoilerPrefs.ts";

/** Home route: match list with spoiler preferences (default tournament window). */
export function Home() {
    const spoilerProps = useSpoilerPrefs();

    return (
        <div className="home container">
            <MatchList spoilerProps={spoilerProps} />
        </div>
    );
}
