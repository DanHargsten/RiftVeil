export type HealthResponse = {
  status: string;
  env: string;
};

async function getJson<T>(url: string): Promise<T> {
  const res = await fetch(url);

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`${res.status} ${res.statusText} - ${text}`);
  }

  return (await res.json()) as T;
}

export function getHealth() {
  return getJson<HealthResponse>("api/health");
}
