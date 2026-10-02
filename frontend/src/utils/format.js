export const formatPrice = (value) =>
  new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value ?? 0);

export const formatDateTime = (iso) => {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return "Date unavailable";
  }
  return date.toLocaleString("en-US", { dateStyle: "medium", timeStyle: "short" });
};
