document.addEventListener("DOMContentLoaded", () => {
    const MONEDA = "Bs.";
    const UMBRAL_STOCK_BAJO = 15;
    const dashboard = document.querySelector(".dashboard-wrapper");
    const mensajeError = document.getElementById("dashboardError");
    const botonRecargar = document.getElementById("btnRecargarDatos");

    // Instancias de gráficas para actualizarlas sin duplicar canvas
    let charts = {};

    // =========================================================================
    // 1. UTILIDADES DE CÁLCULO Y FORMATO
    // =========================================================================
    const calcularPorcentaje = (valor, total) => {
        if (!total || total === 0) return 0;
        return Number(((valor / total) * 100).toFixed(1));
    };

    const formatearMoneda = (valor) =>
        `${MONEDA} ${Number(valor || 0).toLocaleString("es-BO", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

    // =========================================================================
    // 2. ACTUALIZAR TARJETAS KPI Y TABLA DE ALERTAS DE REPOSICIÓN
    // =========================================================================
    function renderizarKPIs(datos) {
        // KPI 1: Ventas de Hoy
        document.getElementById("kpiVentasHoyTotal").textContent = formatearMoneda(datos.ventasHoyTotal);
        document.getElementById("kpiVentasHoyCantidad").textContent = `${datos.ventasHoyCantidad} ventas válidas`;

        // KPI 2: Pedidos En Cola / Barra (Pendientes + En Preparación)
        document.getElementById("kpiPedidosEnCurso").textContent = datos.pedidosEnCursoHoy ?? 0;

        // KPI 3: Stock Total y Alertas
        const productos = datos.productos || [];
        const totalStock = productos.reduce((acc, p) => acc + p.stock, 0);
        const productosCriticos = productos.filter(p => p.stock <= UMBRAL_STOCK_BAJO);

        document.getElementById("kpiStockTotal").textContent = `${totalStock} uds. en total`;
        document.getElementById("kpiStockCritico").textContent = `${productosCriticos.length} con stock bajo`;

        // Tabla de Alertas de Reposición (ordenada de menor a mayor stock)
        const tbody = document.getElementById("tablaAlertasStock");
        tbody.innerHTML = "";
        const alertasOrdenadas = [...productosCriticos].sort((a, b) => a.stock - b.stock);

        if (alertasOrdenadas.length === 0) {
            tbody.innerHTML = `<tr><td colspan="3" class="text-center text-muted py-4">Todo el inventario tiene niveles saludables</td></tr>`;
        } else {
            alertasOrdenadas.forEach(prod => {
                const badgeClass = prod.stock <= 5 ? "bg-danger" : "bg-warning text-dark";
                const fila = document.createElement("tr");

                const celdaNombre = document.createElement("td");
                celdaNombre.className = "ps-3 fw-medium";
                celdaNombre.textContent = prod.nombre;

                const celdaCategoria = document.createElement("td");
                celdaCategoria.className = "text-muted";
                celdaCategoria.textContent = prod.categoria;

                const celdaStock = document.createElement("td");
                celdaStock.className = "text-end pe-3";
                const badge = document.createElement("span");
                badge.className = `badge ${badgeClass}`;
                badge.textContent = `${prod.stock} uds.`;
                celdaStock.appendChild(badge);

                fila.append(celdaNombre, celdaCategoria, celdaStock);
                tbody.appendChild(fila);
            });
        }
    }

    // =========================================================================
    // 3. GRÁFICA PASTEL: ESTADO DE LAS VENTAS (SOLO HOY)
    // =========================================================================
    function renderizarGraficaEstadoVentasHoy(estadosVentasHoy) {
        const tieneDatos = Array.isArray(estadosVentasHoy) && estadosVentasHoy.length > 0;
        const labels = tieneDatos ? estadosVentasHoy.map(e => e.estado) : ["Sin ventas hoy"];
        const valores = tieneDatos ? estadosVentasHoy.map(e => e.cantidad) : [1];
        const total = valores.reduce((a, b) => a + b, 0);

        const coloresEstado = {
            "Completada": "#10b981",
            "Pendiente": "#f59e0b",
            "En Preparación": "#0ea5e9",
            "Anulada": "#64748b",
            "Reembolsada": "#ef4444",
            "Sin ventas hoy": "#e2e8f0"
        };

        if (charts.estadoVentas) charts.estadoVentas.destroy();
        charts.estadoVentas = new Chart(document.getElementById("graficaEstadoVentasHoy"), {
            type: "pie",
            data: {
                labels: labels,
                datasets: [{
                    data: valores,
                    backgroundColor: labels.map(l => coloresEstado[l] || "#64748b"),
                    borderWidth: 2,
                    borderColor: "#ffffff"
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: "bottom" },
                    tooltip: {
                        callbacks: {
                            label: (ctx) => {
                                if (!tieneDatos) return " Sin movimientos registrados hoy";
                                const pct = calcularPorcentaje(ctx.raw, total);
                                return ` ${ctx.label}: ${ctx.raw} ventas (${pct}%)`;
                            }
                        }
                    }
                }
            }
        });
    }

    // =========================================================================
    // 4. GRÁFICA BARRAS HORIZONTALES: TOP 5 PRODUCTOS MÁS VENDIDOS HOY
    // =========================================================================
    function renderizarGraficaTopProductosHoy(topProductosHoy) {
        const tieneDatos = Array.isArray(topProductosHoy) && topProductosHoy.length > 0;
        const labels = tieneDatos ? topProductosHoy.map(p => p.nombre) : ["Sin ventas registradas hoy"];
        const cantidades = tieneDatos ? topProductosHoy.map(p => p.cantidadVendida) : [0];

        // Resalta el producto #1 en azul glaciar intenso y los demás en azul glaciar estándar
        const colores = labels.map((_, idx) => {
            if (!tieneDatos) return "#e2e8f0";
            return idx === 0 ? "#0284c7" : "#0ea5e9";
        });

        if (charts.topProductos) charts.topProductos.destroy();
        charts.topProductos = new Chart(document.getElementById("graficaTopProductosHoy"), {
            type: "bar",
            data: {
                labels: labels,
                datasets: [{
                    label: "Unidades vendidas hoy",
                    data: cantidades,
                    backgroundColor: colores,
                    borderRadius: 6
                }]
            },
            options: {
                indexAxis: "y",
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: (ctx) => {
                                if (!tieneDatos) return " 0 unidades";
                                const extra = ctx.dataIndex === 0 ? " (MÁS PEDIDO)" : "";
                                return ` Vendidos hoy: ${ctx.raw} uds.${extra}`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        ticks: { precision: 0 }
                    }
                }
            }
        });
    }

    // =========================================================================
    // 5. GRÁFICA BARRAS HORIZONTALES: STOCK POR PRODUCTO (RESALTA MAYOR Y BAJO)
    // =========================================================================
    function renderizarGraficaStockProductos(productos) {
        const ordenados = [...productos].sort((a, b) => b.stock - a.stock);
        const labels = ordenados.map(p => p.nombre);
        const stocks = ordenados.map(p => p.stock);

        const colores = ordenados.map((p, idx) => {
            if (p.stock <= UMBRAL_STOCK_BAJO) return "#ef4444"; // Stock bajo (rojo)
            if (idx === 0) return "#10b981"; // Mayor stock (verde)
            return "#38bdf8"; // Normal (celeste glaciar)
        });

        if (charts.stockProd) charts.stockProd.destroy();
        charts.stockProd = new Chart(document.getElementById("graficaStockProductos"), {
            type: "bar",
            data: {
                labels: labels,
                datasets: [{
                    label: "Unidades en Stock",
                    data: stocks,
                    backgroundColor: colores,
                    borderRadius: 6
                }]
            },
            options: {
                indexAxis: "y",
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: (ctx) => {
                                let estado = "";
                                if (ctx.raw <= UMBRAL_STOCK_BAJO) estado = " (STOCK BAJO)";
                                else if (ctx.dataIndex === 0) estado = " (MAYOR STOCK)";
                                return ` Existencias: ${ctx.raw} uds.${estado}`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        ticks: { precision: 0 }
                    }
                }
            }
        });
    }

    // =========================================================================
    // 6. EVENTOS E INICIALIZACIÓN GENERAL
    // =========================================================================
    botonRecargar?.addEventListener("click", inicializarDashboard);

    async function inicializarDashboard() {
        if (!dashboard?.dataset.dashboardUrl) return;

        botonRecargar.disabled = true;
        mensajeError.classList.add("d-none");

        try {
            const respuesta = await fetch(dashboard.dataset.dashboardUrl, {
                headers: { "Accept": "application/json" }
            });
            if (!respuesta.ok) {
                throw new Error(`Error HTTP ${respuesta.status}`);
            }

            const datos = await respuesta.json();
            if (!Array.isArray(datos.productos)
                || !Array.isArray(datos.estadosVentasHoy)
                || !Array.isArray(datos.topProductosHoy)) {
                throw new Error("La respuesta del dashboard no tiene el formato esperado.");
            }

            renderizarKPIs(datos);
            renderizarGraficaEstadoVentasHoy(datos.estadosVentasHoy);
            renderizarGraficaTopProductosHoy(datos.topProductosHoy);
            renderizarGraficaStockProductos(datos.productos);
        } catch (error) {
            console.error("No se pudieron cargar los datos del dashboard.", error);
            mensajeError.textContent = "No se pudieron cargar los datos del turno desde la base de datos. Intenta actualizar la vista nuevamente.";
            mensajeError.classList.remove("d-none");
        } finally {
            botonRecargar.disabled = false;
        }
    }

    inicializarDashboard();
});