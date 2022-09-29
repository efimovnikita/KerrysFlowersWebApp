/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,js}"],
  theme: {
    extend: {
      fontFamily: {
        roboto: "'Roboto Slab', serif",
        montserrat: " 'Montserrat', sans-serif",
      },
      colors: {
        background: "#292524",
        text1: "#44403C",
        text2: "#FAFAF9",
        menu: "#1C1917",
        menutext: "#FDD9C2",
        heading: "#FDC6A2",
        subheading: "#EAB896",
        card: "#E2B4DE",
        secondaryaccent: "#6F296B",
        label: "#B3090A",
      },
    },
  },
  plugins: [],
};
