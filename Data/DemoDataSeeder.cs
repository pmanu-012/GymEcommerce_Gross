using GymEcommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Data;

// Datos de EJEMPLO, solo para desarrollo (se llama únicamente si el entorno es Development).
// Se ejecuta una sola vez: si ya hay alguna categoría cargada, no hace nada.
// Cuando tengas el ABM de categorías y productos, podés borrar estos datos y cargar los reales.
public static class DemoDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.Categorias.AnyAsync())
            return;

        var disciplinas = new[] { "Musculación", "Crossfit", "Funcional", "Running", "Yoga" }
            .ToDictionary(n => n, n => new Disciplina { Nombre = n });

        var grupos = new[] { "Pecho", "Espalda", "Hombros", "Bíceps", "Tríceps", "Piernas", "Glúteos", "Abdomen" }
            .ToDictionary(n => n, n => new GrupoMuscular { Nombre = n });

        var estructura = new Dictionary<string, string[]>
        {
            ["Pesas y barras"] = new[] { "Mancuernas", "Barras y discos", "Kettlebells" },
            ["Máquinas"] = new[] { "Cardio", "Fuerza" },
            ["Accesorios"] = new[] { "Guantes y muñequeras", "Colchonetas", "Bandas elásticas" },
            ["Ropa deportiva"] = new[] { "Remeras", "Calzas", "Calzado" }
        };

        var subcategorias = new Dictionary<string, Subcategoria>();
        foreach (var (nombreCategoria, nombresSub) in estructura)
        {
            var categoria = new Categoria { Nombre = nombreCategoria };
            foreach (var nombreSub in nombresSub)
            {
                var sub = new Subcategoria { Nombre = nombreSub };
                categoria.Subcategorias.Add(sub);
                subcategorias[nombreSub] = sub;
            }
            db.Categorias.Add(categoria);
        }

        db.Disciplinas.AddRange(disciplinas.Values);
        db.GruposMusculares.AddRange(grupos.Values);

        void Agregar(string nombre, string sub, decimal precio, decimal? descuento, int stock,
                     string? uso, string? material, string descripcion, string[] disc, string[] grp)
        {
            var producto = new Producto
            {
                Nombre = nombre,
                Subcategoria = subcategorias[sub],
                Precio = precio,
                PorcentajeDescuento = descuento,
                Stock = stock,
                Uso = uso,
                Material = material,
                Descripcion = descripcion,
                Activo = true
            };
            foreach (var d in disc)
                producto.ProductoDisciplinas.Add(new ProductoDisciplina { Disciplina = disciplinas[d] });
            foreach (var g in grp)
                producto.ProductoGruposMusculares.Add(new ProductoGrupoMuscular { GrupoMuscular = grupos[g] });
            db.Productos.Add(producto);
        }

        var fuerza = new[] { "Musculación", "Funcional" };

        Agregar("Mancuerna hexagonal 10 kg", "Mancuernas", 38500, null, 25, "Entrenamiento de fuerza", "Hierro con recubrimiento de goma",
            "Mancuerna hexagonal con agarre ergonómico y base antideslizante. Se vende por unidad.", fuerza, new[] { "Bíceps", "Hombros", "Pecho" });
        Agregar("Mancuerna hexagonal 20 kg", "Mancuernas", 71500, 10, 12, "Entrenamiento de fuerza", "Hierro con recubrimiento de goma",
            "Mancuerna hexagonal de 20 kg para ejercicios de fuerza. Se vende por unidad.", fuerza, new[] { "Pecho", "Espalda", "Hombros" });
        Agregar("Set de mancuernas ajustables 24 kg", "Mancuernas", 189000, 15, 8, "Entrenamiento en casa", "Acero y plástico ABS",
            "Par de mancuernas con discos intercambiables para ajustar el peso sin ocupar espacio.", fuerza, new[] { "Pecho", "Espalda", "Bíceps", "Tríceps" });
        Agregar("Barra olímpica 20 kg", "Barras y discos", 215000, null, 6, "Levantamiento y fuerza", "Acero cromado",
            "Barra olímpica de 2,20 m con agarre moleteado y rodamientos.", new[] { "Musculación", "Crossfit" }, new[] { "Pecho", "Espalda", "Piernas" });
        Agregar("Discos olímpicos 10 kg (par)", "Barras y discos", 98000, null, 14, "Levantamiento y fuerza", "Hierro fundido",
            "Par de discos olímpicos de 10 kg, compatibles con barras de 50 mm.", new[] { "Musculación", "Crossfit" }, new[] { "Piernas", "Espalda" });
        Agregar("Kettlebell 12 kg", "Kettlebells", 54000, 5, 20, "Entrenamiento funcional", "Hierro fundido con pintura epoxi",
            "Kettlebell con base plana y asa ancha para swings, sentadillas y press.", new[] { "Crossfit", "Funcional" }, new[] { "Glúteos", "Hombros", "Abdomen" });
        Agregar("Cinta de correr plegable", "Cardio", 1250000, 10, 3, "Cardio en casa", "Estructura de acero",
            "Cinta eléctrica plegable con 12 programas y pantalla LCD.", new[] { "Running" }, new[] { "Piernas", "Glúteos" });
        Agregar("Bicicleta fija de spinning", "Cardio", 780000, null, 0, "Cardio indoor", "Acero con volante de inercia",
            "Bicicleta de spinning con resistencia regulable y asiento ajustable.", new[] { "Funcional" }, new[] { "Piernas", "Glúteos" });
        Agregar("Prensa de piernas 45°", "Fuerza", 1890000, null, 2, "Entrenamiento de piernas", "Acero reforzado",
            "Máquina de prensa de piernas con respaldo acolchado y topes de seguridad.", new[] { "Musculación" }, new[] { "Piernas", "Glúteos" });
        Agregar("Guantes de entrenamiento", "Guantes y muñequeras", 18500, null, 40, "Entrenamiento con pesas", "Neopreno y cuero sintético",
            "Guantes con palma acolchada y cierre de velcro.", new[] { "Musculación", "Crossfit" }, Array.Empty<string>());
        Agregar("Muñequeras de soporte", "Guantes y muñequeras", 9800, 20, 60, "Soporte en levantamientos", "Algodón elástico",
            "Par de muñequeras con ajuste de velcro para mayor estabilidad.", new[] { "Musculación" }, Array.Empty<string>());
        Agregar("Colchoneta de yoga 6 mm", "Colchonetas", 24500, null, 35, "Yoga y estiramiento", "TPE antideslizante",
            "Colchoneta liviana con superficie antideslizante.", new[] { "Yoga", "Funcional" }, Array.Empty<string>());
        Agregar("Banda elástica de resistencia", "Bandas elásticas", 12900, null, 50, "Movilidad y fuerza", "Látex natural",
            "Banda de resistencia media para ejercicios de fuerza y rehabilitación.", new[] { "Funcional", "Yoga" }, new[] { "Glúteos", "Hombros" });
        Agregar("Remera deportiva dry-fit", "Remeras", 21900, null, 45, "Entrenamiento", "Poliéster",
            "Remera liviana de secado rápido, con costuras planas.", new[] { "Running", "Funcional" }, Array.Empty<string>());
        Agregar("Calza deportiva de compresión", "Calzas", 32500, 10, 30, "Entrenamiento", "Poliéster y elastano",
            "Calza de compresión media con cintura elástica ancha.", new[] { "Running", "Funcional" }, Array.Empty<string>());
        Agregar("Zapatillas de entrenamiento", "Calzado", 98900, null, 18, "Entrenamiento funcional", "Malla y suela de goma",
            "Zapatillas con base estable y buena amortiguación para entrenar.", new[] { "Crossfit", "Funcional" }, Array.Empty<string>());

        await db.SaveChangesAsync();
    }
}
