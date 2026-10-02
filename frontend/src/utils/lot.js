import { formatDateTime } from "./format.js";

// Mirrors LotStatus from the backend (Auction.DAL.Enums.LotStatus).
export const LOT_STATUS = {
  DRAFT: 0,
  ACTIVE: 1,
  COMPLETED: 2,
  CANCELLED: 3,
};

export const getMainImage = (lot) =>
  lot.images?.find((item) => item.isMain) ?? lot.images?.[0] ?? null;

export const isLotEnded = (lot) => lot.status === LOT_STATUS.COMPLETED;

export const getEndLabel = (lot) =>
  `${isLotEnded(lot) ? "Ended" : "Ends"} ${formatDateTime(lot.endTime)}`;
