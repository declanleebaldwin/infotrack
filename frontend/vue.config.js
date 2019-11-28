module.exports = {
  // The URL where the .Net Core app will be listening.
  // Specific URL depends on whether IISExpress/Kestrel and HTTP/HTTPS are used
  devServer: {
    proxy: {
      '^/api': {
        target: 'https://localhost:44393',
        ws: false
      }
    }
  }
}
