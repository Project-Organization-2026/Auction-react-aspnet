const rawUrl = (import.meta.env.VITE_API_URL || "/api").trim().replace(/\/+$/, "");

export const env = {
  apiUrl: rawUrl.endsWith("/api") ? rawUrl : `${rawUrl}/api`,
};