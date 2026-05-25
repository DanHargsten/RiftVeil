import { useEffect, useId, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import type { GameListItem } from "@/lib/api.ts";

export type MatchDevVodDraft = {
    url: string;
    draftOffsetSeconds: string;
    gameStartOffsetSeconds: string;
};

export interface MatchDevTools {
    busy: boolean;
    message: string | null;
    error: string | null;
    vodDraft: MatchDevVodDraft;
    onVodDraftChange: (draft: MatchDevVodDraft) => void;
    onImportSeries: () => void;
    onSaveVod: () => void;
    onClearVod: () => void;
}

interface MatchDevMenuProps {
    currentGame: GameListItem;
    gameCount: number;
    devTools: MatchDevTools;
}

type PanelPosition = {
    top: number;
    left: number;
    maxHeight: number;
};

const PANEL_WIDTH_PX = 288;
const PANEL_GAP_PX = 6;
const VIEWPORT_MARGIN_PX = 8;

export function MatchDevMenu({ currentGame, gameCount, devTools }: MatchDevMenuProps) {
    const ids = useId();
    const menuId = `${ids}-dev-menu`;
    const [open, setOpen] = useState(false);
    const [panelPosition, setPanelPosition] = useState<PanelPosition | null>(null);
    const rootRef = useRef<HTMLDivElement>(null);
    const triggerRef = useRef<HTMLButtonElement>(null);
    const panelRef = useRef<HTMLDivElement>(null);

    useLayoutEffect(() => {
        if (!open) {
            setPanelPosition(null);
            return;
        }

        function updatePanelPosition() {
            const trigger = triggerRef.current;
            const panel = panelRef.current;
            if (!trigger) return;

            const rect = trigger.getBoundingClientRect();
            const panelWidth = Math.min(PANEL_WIDTH_PX, window.innerWidth - VIEWPORT_MARGIN_PX * 2);
            const panelHeight = panel?.offsetHeight ?? 0;

            let left = rect.right - panelWidth;
            left = Math.max(
                VIEWPORT_MARGIN_PX,
                Math.min(left, window.innerWidth - panelWidth - VIEWPORT_MARGIN_PX),
            );

            const spaceBelow = window.innerHeight - rect.bottom - PANEL_GAP_PX - VIEWPORT_MARGIN_PX;
            const spaceAbove = rect.top - PANEL_GAP_PX - VIEWPORT_MARGIN_PX;
            const openBelow = spaceBelow >= panelHeight || spaceBelow >= spaceAbove;
            const maxHeight = Math.max(120, openBelow ? spaceBelow : spaceAbove);
            const top = openBelow
                ? rect.bottom + PANEL_GAP_PX
                : Math.max(VIEWPORT_MARGIN_PX, rect.top - PANEL_GAP_PX - Math.min(panelHeight, maxHeight));

            setPanelPosition({ top, left, maxHeight });
        }

        updatePanelPosition();
        const raf = requestAnimationFrame(updatePanelPosition);

        const panel = panelRef.current;
        const resizeObserver = panel ? new ResizeObserver(updatePanelPosition) : null;
        if (panel && resizeObserver)
            resizeObserver.observe(panel);

        window.addEventListener("resize", updatePanelPosition);
        window.addEventListener("scroll", updatePanelPosition, true);
        return () => {
            cancelAnimationFrame(raf);
            resizeObserver?.disconnect();
            window.removeEventListener("resize", updatePanelPosition);
            window.removeEventListener("scroll", updatePanelPosition, true);
        };
    }, [open, devTools.message, devTools.error]);

    useEffect(() => {
        if (!open) return;

        function handlePointerDown(event: MouseEvent) {
            const target = event.target as Node;
            if (rootRef.current?.contains(target) || panelRef.current?.contains(target))
                return;
            setOpen(false);
        }

        function handleEscape(event: KeyboardEvent) {
            if (event.key === "Escape")
                setOpen(false);
        }

        document.addEventListener("mousedown", handlePointerDown);
        document.addEventListener("keydown", handleEscape);
        return () => {
            document.removeEventListener("mousedown", handlePointerDown);
            document.removeEventListener("keydown", handleEscape);
        };
    }, [open]);

    const urlFieldId = `${ids}-vod-url`;
    const draftOffsetFieldId = `${ids}-vod-draft-offset`;
    const gameStartOffsetFieldId = `${ids}-vod-game-offset`;

    const panel = open ? (
        <div
            id={menuId}
            ref={panelRef}
            className="match-detail__dev-menu-panel match-detail__dev-menu-panel--floating"
            role="dialog"
            aria-label="Developer tools"
            style={{
                visibility: panelPosition ? "visible" : "hidden",
                top: panelPosition?.top ?? 0,
                left: panelPosition?.left ?? 0,
                maxHeight: panelPosition?.maxHeight ?? 400,
            }}
        >
            <p className="match-detail__dev-menu-note">
                Local dev only — not shown in production.
            </p>

            {/* IMPORT */}
            <div className="match-detail__dev-menu-section">
                <span className="match-detail__dev-menu-section-title">Leaguepedia</span>
                <button
                    type="button"
                    className="match-detail__dev-menu-action"
                    disabled={devTools.busy || gameCount === 0}
                    onClick={() => {
                        devTools.onImportSeries();
                    }}
                >
                    Import details — all {gameCount} games
                </button>
            </div>

            {/* MANUAL VOD */}
            <div className="match-detail__dev-menu-section">
                <span className="match-detail__dev-menu-section-title">
                    Manual VOD — Game {currentGame.gameNumber}
                </span>
                <p className="match-detail__dev-menu-hint">
                    Opens the broadcast video (YouTube/Twitch), not the draft table below.
                    Copy timestamps from the video progress bar (e.g. 1:03 or 6:21).
                </p>
                <label className="match-detail__dev-menu-field" htmlFor={urlFieldId}>
                    <span className="match-detail__dev-menu-field-label">URL</span>
                    <input
                        id={urlFieldId}
                        type="url"
                        className="match-detail__dev-menu-input"
                        placeholder="https://youtube.com/watch?v=…"
                        value={devTools.vodDraft.url}
                        disabled={devTools.busy}
                        onChange={(event) => devTools.onVodDraftChange({
                            ...devTools.vodDraft,
                            url: event.target.value,
                        })}
                    />
                </label>
                <label className="match-detail__dev-menu-field" htmlFor={draftOffsetFieldId}>
                    <span className="match-detail__dev-menu-field-label">VOD · draft</span>
                    <input
                        id={draftOffsetFieldId}
                        type="text"
                        inputMode="numeric"
                        className="match-detail__dev-menu-input match-detail__dev-menu-input--time"
                        placeholder="0"
                        autoComplete="off"
                        spellCheck={false}
                        value={devTools.vodDraft.draftOffsetSeconds}
                        disabled={devTools.busy}
                        onChange={(event) => devTools.onVodDraftChange({
                            ...devTools.vodDraft,
                            draftOffsetSeconds: event.target.value,
                        })}
                    />
                </label>
                <label className="match-detail__dev-menu-field" htmlFor={gameStartOffsetFieldId}>
                    <span className="match-detail__dev-menu-field-label">VOD · game</span>
                    <input
                        id={gameStartOffsetFieldId}
                        type="text"
                        inputMode="numeric"
                        className="match-detail__dev-menu-input match-detail__dev-menu-input--time"
                        placeholder="6:21"
                        autoComplete="off"
                        spellCheck={false}
                        value={devTools.vodDraft.gameStartOffsetSeconds}
                        disabled={devTools.busy}
                        onChange={(event) => devTools.onVodDraftChange({
                            ...devTools.vodDraft,
                            gameStartOffsetSeconds: event.target.value,
                        })}
                    />
                </label>
                <div className="match-detail__dev-menu-actions">
                    <button
                        type="button"
                        className="match-detail__dev-menu-action match-detail__dev-menu-action--primary"
                        disabled={devTools.busy}
                        onClick={() => {
                            devTools.onSaveVod();
                        }}
                    >
                        Save VOD
                    </button>
                    <button
                        type="button"
                        className="match-detail__dev-menu-action"
                        disabled={devTools.busy}
                        onClick={() => {
                            devTools.onClearVod();
                        }}
                    >
                        Clear
                    </button>
                </div>
            </div>

            {devTools.message ? (
                <p className="match-detail__dev-menu-feedback match-detail__dev-menu-feedback--ok" role="status">
                    {devTools.message}
                </p>
            ) : null}
            {devTools.error ? (
                <p className="match-detail__dev-menu-feedback match-detail__dev-menu-feedback--error" role="alert">
                    {devTools.error}
                </p>
            ) : null}
        </div>
    ) : null;

    return (
        <div className="match-detail__dev-menu" ref={rootRef}>
            <button
                ref={triggerRef}
                type="button"
                className="match-detail__dev-menu-trigger"
                aria-expanded={open}
                aria-controls={menuId}
                aria-label="Developer tools (local only)"
                onClick={() => setOpen((prev) => !prev)}
            >
                <span className="match-detail__dev-menu-trigger-icon" aria-hidden="true">⋮</span>
                <span className="match-detail__dev-menu-trigger-label">Dev</span>
            </button>

            {panel && typeof document !== "undefined"
                ? createPortal(panel, document.body)
                : null}
        </div>
    );
}
