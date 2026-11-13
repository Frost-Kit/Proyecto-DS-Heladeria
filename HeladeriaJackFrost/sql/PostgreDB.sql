-- ============================================================================
-- PROYECTO: Sistema de Gestión - Heladería "Jack Frost"
-- MOTOR: PostgreSQL
-- ARQUITECTURA: 3FN (9 Tablas alineadas 1:1 con el Diagrama de Clases UML)
-- ============================================================================

CREATE database jackfrost_db;

-- ============================================================================
-- 1. MÓDULO DE ABASTECIMIENTO E INVENTARIO (HU-02 y JK-01)
-- ============================================================================

-- Tabla: proveedor (Responde a HU-02)
-- Estado: INT del 1 al 5 (ej. 1=Activo, 2=Inactivo, 3=Suspendido, etc.)
CREATE TABLE proveedor (
    id_proveedor SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL UNIQUE,
    nombre_contacto VARCHAR(100) NOT NULL,
    telefono VARCHAR(20) NOT NULL,
    direccion VARCHAR(150) NOT NULL,
    estado INT NOT NULL DEFAULT 1 CHECK (estado BETWEEN 1 AND 5)
);

-- Tabla: categoria (Normalización 3FN para el filtro de JK-01)
CREATE TABLE categoria (
    id_categoria SERIAL PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL UNIQUE
);

-- Tabla: producto (Responde a JK-01)
CREATE TABLE producto (
    id_producto SERIAL PRIMARY KEY,
    id_categoria INT NOT NULL,
    nombre VARCHAR(100) NOT NULL UNIQUE,
    precio NUMERIC(10, 2) NOT NULL CHECK (precio > 0),
    stock INT NOT NULL DEFAULT 0 CHECK (stock >= 0),
    CONSTRAINT fk_producto_categoria
        FOREIGN KEY (id_categoria) REFERENCES categoria (id_categoria)
        ON UPDATE CASCADE ON DELETE RESTRICT
);

-- Tabla intermedia: compra (Abastecimiento Proveedor -> Producto)
CREATE TABLE compra (
    id_compra SERIAL PRIMARY KEY,
    id_proveedor INT NOT NULL,
    id_producto INT NOT NULL,
    fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    cantidad INT NOT NULL CHECK (cantidad > 0),
    costo_unitario NUMERIC(10, 2) NOT NULL CHECK (costo_unitario > 0),
    total NUMERIC(10, 2) NOT NULL CHECK (total > 0),
    CONSTRAINT fk_compra_proveedor
        FOREIGN KEY (id_proveedor) REFERENCES proveedor (id_proveedor)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_compra_producto
        FOREIGN KEY (id_producto) REFERENCES producto (id_producto)
        ON UPDATE CASCADE ON DELETE RESTRICT
);

-- ============================================================================
-- 2. MÓDULO DE PERSONAS - HERENCIA (Persona <- Cliente / Personal)
-- ============================================================================

-- Superclase: persona
CREATE TABLE persona (
    id_persona SERIAL PRIMARY KEY,
    nombre VARCHAR(80) NOT NULL,
    apellido_pat VARCHAR(80) NOT NULL,
    apellido_mat VARCHAR(80),
    telefono VARCHAR(20)
);

-- Subclase: cuenta (para el acceso al sistema | Persona 1 -> N cuenta )
CREATE TABLE cuenta (
    id_cuenta SERIAL PRIMARY KEY,
    id_persona INT NOT NULL,
    email VARCHAR(80) NOT NULL UNIQUE,
    usuario VARCHAR(80) NOT NULL UNIQUE,
    contra VARCHAR(255),
    CONSTRAINT fk_cuenta_persona
        FOREIGN KEY (id_persona) REFERENCES persona (id_persona)
            ON UPDATE CASCADE ON DELETE CASCADE
    --CONSTRAINT uk_persona_usuario UNIQUE (id_persona, usuario) -- con solo los UNIQUE en email y usuario bastaria, creo...
);

-- Subclase: cliente (Relación 1 a 1 con persona)
CREATE TABLE cliente (
    id_cliente SERIAL PRIMARY KEY,
    id_persona INT NOT NULL UNIQUE,
    puntos_de_cliente INT NOT NULL DEFAULT 0 CHECK (puntos_de_cliente >= 0),
    CONSTRAINT fk_cliente_persona
        FOREIGN KEY (id_persona) REFERENCES persona (id_persona)
        ON UPDATE CASCADE ON DELETE CASCADE
);

-- Subclase: personal (Relación 1 a 1 con persona)
-- Estado: INT del 1 al 5 (ej. 1=En turno, 2=Descanso, 3=Vacaciones, 4=Licencia, 5=Retirado)
CREATE TABLE personal (
    id_personal SERIAL PRIMARY KEY,
    id_persona INT NOT NULL UNIQUE,
    cargo VARCHAR(50) NOT NULL,
    estado INT NOT NULL DEFAULT 1 CHECK (estado BETWEEN 1 AND 5),
    CONSTRAINT fk_personal_persona
        FOREIGN KEY (id_persona) REFERENCES persona (id_persona)
        ON UPDATE CASCADE ON DELETE CASCADE
);

-- ============================================================================
-- 3. MÓDULO DE VENTAS Y FACTURACIÓN (JK-03)
-- ============================================================================

-- Tabla: venta (Cabecera de la transacción)
-- Estado: INT del 1 al 5 (ej. 1=Completada, 2=Pendiente, 3=En preparación, 4=Anulada, 5=Reembolsada)
CREATE TABLE venta (
    id_venta SERIAL PRIMARY KEY,
    id_cliente INT NOT NULL,
    id_personal INT NOT NULL,
    fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    total NUMERIC(10, 2) NOT NULL DEFAULT 0.00 CHECK (total >= 0),
    estado INT NOT NULL DEFAULT 1 CHECK (estado BETWEEN 1 AND 5),
    CONSTRAINT fk_venta_cliente
        FOREIGN KEY (id_cliente) REFERENCES cliente (id_cliente)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_venta_personal
        FOREIGN KEY (id_personal) REFERENCES personal (id_personal)
        ON UPDATE CASCADE ON DELETE RESTRICT
);

-- Tabla intermedia: detalle (Ítems de la venta; valida cantidad > 0 según JK-03)
CREATE TABLE detalle (
    id_detalle SERIAL PRIMARY KEY,
    id_venta INT NOT NULL,
    id_producto INT NOT NULL,
    cantidad INT NOT NULL CHECK (cantidad > 0),
    precio_unitario NUMERIC(10, 2) NOT NULL CHECK (precio_unitario > 0),
    descuento NUMERIC(10, 2) NOT NULL DEFAULT 0.00 CHECK (descuento >= 0),
    subtotal NUMERIC(10, 2) NOT NULL CHECK (subtotal > 0),
    CONSTRAINT fk_detalle_venta
        FOREIGN KEY (id_venta) REFERENCES venta (id_venta)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT fk_detalle_producto
        FOREIGN KEY (id_producto) REFERENCES producto (id_producto)
        ON UPDATE CASCADE ON DELETE RESTRICT
);

-- ============================================================================
-- 4. DATOS SEMILLA AMPLIADOS (SEED DATA MASIVO PARA PRUEBAS CRUD)
-- ============================================================================

-- 1. Categorías (6 categorías para probar filtros y ordenamientos en JK-01)
INSERT INTO categoria (nombre) VALUES
('Cremas Clásicas'),
('Cremas Especiales'),
('Frutales al Agua'),
('Postres Helados'),
('Potes Familiares 2L'),
('Línea Sin Azúcar / Keto');

-- 2. Proveedores (6 proveedores con distintos estados del 1 al 5 para HU-02)
INSERT INTO proveedor (nombre, nombre_contacto, telefono, direccion, estado) VALUES
('Helados Artesanales del Sur S.R.L.', 'Carlos Méndez', '71234567', 'Av. Las Américas #450', 1),
('Distribuidora Frío Polar', 'María Fernanda Rojas', '72987654', 'Calle Sucre #820', 1),
('Lácteos y Helados del Valle', 'Jorge Luis Quiroga', '76543210', 'Av. Integración Km 3', 1),
('Importadora Dulce Nieve', 'Lucía Castellanos', '78451236', 'Barrio Senac Calle 4', 2),
('Sabores Andinos Mayorista', 'Roberto Aparicio', '71852963', 'Av. Circunvalación #1120', 3),
('Envases y Congelados Tarija', 'Valeria Ortíz', '74125896', 'Calle Colón #315', 4);

-- 3. Productos (12 productos variados, incluyendo varios con stock 0 para probar agotados en JK-01)
INSERT INTO producto (id_categoria, nombre, precio, stock) VALUES
(1, 'Pote 1L Crema Americana', 35.00, 15),
(1, 'Pote 1L Dulce de Leche Granizado', 38.00, 20),
(1, 'Pote 1L Vainilla Francesa', 35.00, 0),
(2, 'Pote 1L Pistacho Imperial', 48.00, 12),
(2, 'Pote 1L Chocolate Suizo con Almendras', 45.00, 9),
(2, 'Pote 1L Mascarpone con Frutos Rojos', 46.50, 0),
(3, 'Pote 1L Frutos del Bosque al Agua', 36.00, 14),
(3, 'Pote 1L Maracuyá Tropical', 34.00, 18),
(3, 'Pote 1L Limón a la Menta', 32.00, 5),
(4, 'Torta Helada Oreo 8 Porciones', 85.00, 6),
(5, 'Balde Familiar 2L Napolitano Especial', 65.00, 10),
(6, 'Pote 500ml Chocolate 70% Cacao Sin Azúcar', 42.00, 0);

-- 4. Personas (12 personas: 4 para Personal y 8 para Clientes)
INSERT INTO persona (nombre, apellido_pat, apellido_mat, telefono) VALUES
-- Personal (IDs 1 al 4)
('Andrés', 'Vargas', 'Salazar', '75544332'),
('Camila', 'Flores', 'Gutiérrez', '76611223'),
('Diego', 'Camacho', 'Soruco', '71122334'),
('Sofía', 'Aramayo', 'Paz', '78899001'),
-- Clientes (IDs 5 al 12)
('Cliente', 'Mostrador', 'General', '00000000'),
('Martín', 'Reyes', 'Dorado', '72233445'),
('Gabriela', 'Zenteno', 'Lema', '73344556'),
('Fernando', 'Castrillo', 'Vaca', '74455667'),
('Daniela', 'Ruíz', 'Baldiviezo', '79988776'),
('Alejandro', 'Molina', 'Trigo', '71593570'),
('Patricia', 'navarro', 'Hoyos', '76321458'),
('Rodrigo', 'Pacheco', 'Ávila', '77412369');

-- 5. Personal (4 empleados con distintos cargos y estados del 1 al 3)
INSERT INTO personal (id_persona, cargo, estado) VALUES
(1, 'Cajero Principal', 1),
(2, 'Administrador', 1),
(3, 'Encargado de Compras y Almacén', 1),
(4, 'Cajero Turno Noche', 2);

-- 6. Clientes (8 clientes registrados con distintos puntos acumulados)
INSERT INTO cliente (id_persona, puntos_de_cliente) VALUES
(5, 0),
(6, 120),
(7, 85),
(8, 240),
(9, 40),
(10, 310),
(11, 15),
(12, 95);

-- 7. Compras a Proveedores (9 registros de abastecimiento de helados ya hechos)
INSERT INTO compra (id_proveedor, id_producto, fecha, cantidad, costo_unitario, total) VALUES
(1, 1, '2026-11-01 08:30:00', 15, 22.00, 330.00),
(1, 2, '2026-11-01 08:35:00', 20, 24.00, 480.00),
(2, 4, '2026-11-02 09:15:00', 12, 30.00, 360.00),
(2, 5, '2026-11-02 09:20:00', 10, 28.00, 280.00),
(3, 7, '2026-11-03 10:00:00', 14, 21.00, 294.00),
(3, 8, '2026-11-03 10:05:00', 18, 20.00, 360.00),
(4, 10, '2026-11-05 11:30:00', 6, 55.00, 330.00),
(5, 11, '2026-11-06 15:00:00', 10, 42.00, 420.00),
(6, 9, '2026-11-08 16:45:00', 8, 19.00, 152.00);

-- 8. Ventas (9 transacciones con estados variados del 1 al 4 para probar JK-03)
INSERT INTO venta (id_cliente, id_personal, fecha, total, estado) VALUES
(1, 1, '2026-11-09 14:20:00', 96.00, 1),
(2, 1, '2026-11-09 15:45:00', 73.00, 1),
(3, 4, '2026-11-09 19:10:00', 85.00, 1),
(4, 4, '2026-11-09 20:30:00', 130.00, 1),
(5, 1, '2026-11-10 13:15:00', 104.00, 2),
(6, 1, '2026-11-10 16:00:00', 110.00, 1),
(7, 4, '2026-11-10 18:50:00', 45.00, 3),
(8, 4, '2026-11-10 21:05:00', 68.00, 4),
(2, 1, '2026-11-11 10:30:00', 155.00, 1);

-- 9. Detalles de Venta (12 líneas de detalle cuadradas exactamente con el total de cada venta)
INSERT INTO detalle (id_venta, id_producto, cantidad, precio_unitario, descuento, subtotal) VALUES
-- Venta 1 (Total: 96.00)
(1, 4, 2, 48.00, 0.00, 96.00),
-- Venta 2 (Total: 73.00 -> 35.00 + 38.00)
(2, 1, 1, 35.00, 0.00, 35.00),
(2, 2, 1, 38.00, 0.00, 38.00),
-- Venta 3 (Total: 85.00)
(3, 10, 1, 85.00, 0.00, 85.00),
-- Venta 4 (Total: 130.00)
(4, 11, 2, 65.00, 0.00, 130.00),
-- Venta 5 (Total: 104.00 -> 72.00 + 32.00)
(5, 7, 2, 36.00, 0.00, 72.00),
(5, 9, 1, 32.00, 0.00, 32.00),
-- Venta 6 (Total: 110.00 -> 3 potes de 38.00 = 114.00 menos 4.00 de descuento)
(6, 2, 3, 38.00, 4.00, 110.00),
-- Venta 7 (Total: 45.00)
(7, 5, 1, 45.00, 0.00, 45.00),
-- Venta 8 (Total: 68.00)
(8, 8, 2, 34.00, 0.00, 68.00),
-- Venta 9 (Total: 155.00 -> 85.00 + 70.00)
(9, 10, 1, 85.00, 0.00, 85.00),
(9, 1, 2, 35.00, 0.00, 70.00);

