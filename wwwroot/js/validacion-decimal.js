// jQuery Validation espera números con punto ("1234.50"), pero en es-AR se escribe "1.234,50".
// Este archivo hace que la validación del navegador acepte el formato argentino.
// Se carga después de jquery.validate y jquery.validate.unobtrusive.
(() => {
    if (!window.jQuery || !jQuery.validator) return;

    const aNumero = (valor) => parseFloat(String(valor).replace(/\./g, '').replace(',', '.'));

    jQuery.validator.methods.number = function (value, element) {
        return this.optional(element) || /^-?(?:\d+|\d{1,3}(?:\.\d{3})+)(?:,\d+)?$/.test(value);
    };

    jQuery.validator.methods.range = function (value, element, param) {
        const numero = aNumero(value);
        return this.optional(element) || (numero >= param[0] && numero <= param[1]);
    };
})();
