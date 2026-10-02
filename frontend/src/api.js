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

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && localStorage.getItem("token")) {
      // Token is expired or invalid
      localStorage.removeItem("token");
      window.dispatchEvent(new Event("auth:logout"));
    }
    return Promise.reject(error);
  }
);

export const authApi = {
  login: async (email, password) => {
    const res = await api.post("/auth/login", { email, password });
    return res.data; // { token }
  },
  register: async (userName, email, password) => {
    const res = await api.post("/auth/register", { userName, email, password });
    return res.data; // { token }
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
};