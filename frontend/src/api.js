import axios from "axios";
import { env } from "./env.js";

export const api = axios.create({
    baseURL: env.apiUrl
});

api.interceptors.request.use(cfg => {
    const token = localStorage.getItem("token");
    if (token) {
        cfg.headers.Authorization = `Bearer ${token}`;
    }
    return cfg;
});