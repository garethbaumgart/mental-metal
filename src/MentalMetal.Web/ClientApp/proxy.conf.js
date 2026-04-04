module.exports = {
  "/api/**": {
    target: process.env.API_TARGET || "http://localhost:5289",
    secure: false,
    changeOrigin: true,
  },
};
