// Ayudantes de SweetAlert2 para todo el panel de administración.
// SweetAlert2 solo muestra alertas cuando algún código llama explícitamente a los métodos de Alertas (desde una vista Razor por ejemplo).
// Estas estas se cargan en el _layout y en el _LayoutAdmin.cshtml y se pueden usar en cualquier vista Razor.

//modulo global que encapsula los métodos de SweetAlert2 para mostrar alertas y confirmaciones.
const Alertas = (() => {
    const aviso = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3500,
        timerProgressBar: true
    });

    return {
        exito: (mensaje) => aviso.fire({ icon: 'success', title: mensaje }),
        error: (mensaje) => aviso.fire({ icon: 'error', title: mensaje }),
         aviso: (titulo, texto) =>
                    Swal.fire({
                        icon: 'info',
                        title: titulo,
                        text: texto,
                        confirmButtonText: 'Entendido',
                        confirmButtonColor: '#FF5B14'
                    }),

        // Devuelve una promesa que resuelve true si la persona confirma.
        confirmar: ({ titulo, texto, confirmar = 'Aceptar' }) =>
            Swal.fire({
                title: titulo,
                text: texto,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: confirmar,
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#FF5B14',
                cancelButtonColor: '#6B7280',
                reverseButtons: true,
                focusCancel: true
            }).then(resultado => resultado.isConfirmed)
    };
})();
