window.hurricaneSite = {
  saveCheckout(payload) {
    window.sessionStorage.setItem("hurricane-checkout", payload);
  },
  loadCheckout() {
    return window.sessionStorage.getItem("hurricane-checkout");
  },
  clearCheckout() {
    window.sessionStorage.removeItem("hurricane-checkout");
  },
  redirect(url) {
    window.location.assign(url);
  }
};
