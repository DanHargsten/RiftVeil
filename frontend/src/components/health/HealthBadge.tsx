import { useQuery } from "@tanstack/react-query";
import { getHealth } from "@/lib/api";

export function HealthBadge() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["health"],
    queryFn: getHealth,
  });

  if (isLoading) return <span>API: ...</span>;
  if (error) return <span>API: ❌</span>;

  return <span>API: ✅ ({data?.env})</span>;
}
