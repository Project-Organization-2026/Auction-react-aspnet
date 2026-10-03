import axios from "axios";
import { env } from "./env.js";

export const api = axios.create({
  baseURL: env.apiUrl,
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !originalRequest.url?.includes("/auth/login") &&
      !originalRequest.url?.includes("/auth/register") &&
      !originalRequest.url?.includes("/auth/refresh")
    ) {
      const accessToken = localStorage.getItem("token");
      const refreshToken = localStorage.getItem("refreshToken");

      if (!refreshToken) {
        localStorage.removeItem("token");
        localStorage.removeItem("refreshToken");
        window.dispatchEvent(new Event("auth:logout"));
        return Promise.reject(error);
      }

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return api(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const response = await axios.post(`${env.apiUrl}/auth/refresh`, {
          accessToken,
          refreshToken,
        });

        const newAccessToken = response.data?.accessToken;
        const newRefreshToken = response.data?.refreshToken;

        if (newAccessToken) {
          localStorage.setItem("token", newAccessToken);
          if (newRefreshToken) {
            localStorage.setItem("refreshToken", newRefreshToken);
          }
          api.defaults.headers.common["Authorization"] = `Bearer ${newAccessToken}`;
          originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
          processQueue(null, newAccessToken);
          return api(originalRequest);
        } else {
          throw new Error("No access token returned from refresh endpoint.");
        }
      } catch (refreshError) {
        processQueue(refreshError, null);
        localStorage.removeItem("token");
        localStorage.removeItem("refreshToken");
        window.dispatchEvent(new Event("auth:logout"));
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export const authApi = {
  login: async (email, password) => {
    const res = await api.post("/auth/login", { email, password });
    return res.data; // { accessToken, refreshToken, user }
  },
  register: async (userName, email, password) => {
    const res = await api.post("/auth/register", { userName, email, password });
    return res.data; // { accessToken, refreshToken, user }
  },
  refresh: async (accessToken, refreshToken) => {
    const res = await axios.post(`${env.apiUrl}/auth/refresh`, { accessToken, refreshToken });
    return res.data;
  },
  revoke: async () => {
    const res = await api.post("/auth/revoke");
    return res.data;
  },
};

export const usersApi = {
  getMe: async () => {
    const res = await api.get("/users/me");
    return res.data;
  },
  updateProfile: async (data) => {
    const res = await api.put("/users/me", data);
    return res.data;
  },
  topUpBalance: async (amount) => {
    const res = await api.post("/users/me/balance", { amount: Number(amount) });
    return res.data; // { balance }
  },
  getMyBids: async () => {
    const res = await api.get("/users/me/bids");
    return res.data;
  },
  getMyWonLots: async () => {
    const res = await api.get("/users/me/won-lots");
    return res.data;
  },
};

export const categoriesApi = {
  getAll: async () => {
    const res = await api.get("/categories");
    return res.data;
  },
  getById: async (id) => {
    const res = await api.get(`/categories/${id}`);
    return res.data;
  },
  create: async (data) => {
    const res = await api.post("/categories", data);
    return res.data;
  },
};

export const lotsApi = {
  getAll: async ({ page = 1, pageSize = 12, search, categoryId, status } = {}) => {
    const params = new URLSearchParams();
    if (page) params.append("page", page);
    if (pageSize) params.append("pageSize", pageSize);
    if (search && search.trim()) params.append("search", search.trim());
    if (categoryId) params.append("categoryId", categoryId);
    if (status !== undefined && status !== null && status !== "all") params.append("status", status);

    const res = await api.get(`/lots?${params.toString()}`);
    return res.data; // PagedResultDto: { items, page, pageSize, totalCount, totalPages, hasNextPage }
  },
  getById: async (id) => {
    const res = await api.get(`/lots/${id}`);
    return res.data;
  },
  create: async (data) => {
    const res = await api.post("/lots", data);
    return res.data;
  },
  update: async (id, data) => {
    const res = await api.put(`/lots/${id}`, data);
    return res.data;
  },
  delete: async (id) => {
    const res = await api.delete(`/lots/${id}`);
    return res.data;
  },
  close: async (id) => {
    const res = await api.post(`/lots/${id}/close`);
    return res.data;
  },
  addImage: async (lotId, { url, isMain }) => {
    const res = await api.post(`/lots/${lotId}/images`, { url, isMain });
    return res.data;
  },
  uploadImage: async (lotId, file, isMain = false) => {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("isMain", String(isMain));
    const res = await api.post(`/lots/${lotId}/images/upload`, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    return res.data;
  },
  deleteImage: async (imageId) => {
    const res = await api.delete(`/lots/images/${imageId}`);
    return res.data;
  },
  setMainImage: async (lotId, imageId) => {
    const res = await api.patch(`/lots/${lotId}/images/${imageId}/set-main`);
    return res.data;
  },
};

export const bidsApi = {
  getByLotId: async (lotId, page = 1, pageSize = 20) => {
    const res = await api.get(`/lots/${lotId}/bids?page=${page}&pageSize=${pageSize}`);
    return res.data; // PagedResultDto: { items, totalCount, ... }
  },
  create: async (lotId, amount) => {
    const res = await api.post("/bids", {
      lotId: Number(lotId),
      amount: Number(amount),
    });
    return res.data;
  },
  /** Register an already-confirmed on-chain ETH bid on the platform backend. */
  createOnChain: async ({ lotId, txHash, amountEth, walletAddress }) => {
    const res = await api.post("/bids/on-chain", {
      lotId: Number(lotId),
      txHash,
      amountEth: Number(amountEth),
      walletAddress,
    });
    return res.data; // BidDto
  },
};