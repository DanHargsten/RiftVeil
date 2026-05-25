import { useState } from "react";
import { AdminBackfillTab } from "@/components/Admin/AdminBackfillTab.tsx";
import { AdminGameVodsTab } from "@/components/Admin/AdminGameVodsTab.tsx";
import { AdminImportTab } from "@/components/Admin/AdminImportTab.tsx";
import { AdminTeamsTab } from "@/components/Admin/AdminTeamsTab.tsx";

type AdminTab = "import" | "backfill" | "teams" | "vods";

const TABS: { id: AdminTab; label: string }[] = [
    { id: "import", label: "Import" },
    { id: "backfill", label: "Repair" },
    { id: "teams", label: "Teams" },
    { id: "vods", label: "Game VODs" },
];

export function Admin() {
    const [activeTab, setActiveTab] = useState<AdminTab>("import");

    return (
        <div className="page">
            <div className={`admin admin--${activeTab}`}>
                <h1 className="admin__title">Admin</h1>

                <div className="admin__tabs" role="tablist" aria-label="Admin sections">
                    {TABS.map((tab) => (
                        <button
                            key={tab.id}
                            type="button"
                            role="tab"
                            id={`admin-tab-${tab.id}`}
                            aria-selected={activeTab === tab.id}
                            aria-controls={`admin-panel-${tab.id}`}
                            className={`admin__tab${activeTab === tab.id ? " admin__tab--active" : ""}`}
                            onClick={() => setActiveTab(tab.id)}
                        >
                            {tab.label}
                        </button>
                    ))}
                </div>

                <div
                    role="tabpanel"
                    id={`admin-panel-${activeTab}`}
                    aria-labelledby={`admin-tab-${activeTab}`}
                >
                    {activeTab === "import" && <AdminImportTab />}
                    {activeTab === "backfill" && <AdminBackfillTab />}
                    {activeTab === "teams" && <AdminTeamsTab />}
                    {activeTab === "vods" && <AdminGameVodsTab />}
                </div>
            </div>
        </div>
    );
}
