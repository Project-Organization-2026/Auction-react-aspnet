import { formatDateTime } from "./format.js";

// Mirrors LotStatus from the backend (Auction.DAL.Enums.LotStatus).
export const LOT_STATUS = {
  DRAFT: 0,
  ACTIVE: 1,
  COMPLETED: 2,
  CANCELLED: 3,
};

export const getMainImage = (lot) =>
  lot?.images?.find((item) => item.isMain) ?? lot?.images?.[0] ?? null;

export const isLotEnded = (lot) => {
  if (!lot) return true;
  if (lot.status === LOT_STATUS.COMPLETED || lot.status === LOT_STATUS.CANCELLED) {
    return true;
  }
  if (lot.endTime && new Date(lot.endTime).getTime() <= Date.now()) {
    return true;
  }
  return false;
};

export const getEndLabel = (lot) => {
  if (!lot) return "";
  return `${isLotEnded(lot) ? "Ended" : "Ends"} ${formatDateTime(lot.endTime)}`;
};
