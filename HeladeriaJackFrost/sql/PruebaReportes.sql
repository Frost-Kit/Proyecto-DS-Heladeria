-- Para ver los reportes si coinciden, talvez hacerlo un PS
WITH reporte_mes_noviembre AS (
    SELECT
        v.id_venta,
        v.fecha,
        concat_ws(' ', pc.nombre, pc.apellido_pat, pc.apellido_mat) AS cliente,
        concat_ws(' ', pv.nombre, pv.apellido_pat, pv.apellido_mat) AS vendedor,
        v.subtotal_bruto,
        v.monto_descuento,
        v.total
    FROM venta v
             INNER JOIN cliente c ON v.id_cliente = c.id_cliente
             INNER JOIN persona pc ON c.id_persona = pc.id_persona
             INNER JOIN personal per ON v.id_personal = per.id_personal
             INNER JOIN persona pv ON per.id_persona = pv.id_persona
    WHERE v.fecha::date BETWEEN '2026-11-1'::date AND '2026-11-30'::date
      AND v.estado NOT IN (4, 5)
    ORDER BY v.fecha ASC, v.id_venta ASC
)
SELECT
    SUM(subtotal_bruto) AS "Total_subtotal_bruto",
    SUM(monto_descuento) AS Total_monto_descuento,
    SUM(total) AS Total_Final
FROM reporte_mes_noviembre;
