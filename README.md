# Workshop 06 - Flat Files and Graphs (Delivery Routing)

Console application in C# (.NET) that models a city street network as a directed weighted graph to compute the shortest valid pickup-and-delivery route for a courier.

## How to Run


   ```bash
Posición inicial: A
Paquetes: 3 Ubicaciones: 7
Calles: 12 (1 de un solo sentido)
1. Ir de A a E (550 m): recoger paquete 2.
Camino: A -> B -> E
2. Ir de E a C (350 m): recoger paquete 1.
Camino: E -> B -> C
3. Ir de C a D (200 m): entregar paquete 2.
Camino: C -> D
4. Ir de D a G (520 m): entregar paquete 1 y recoger paquete 3.
Camino: D -> F -> G
5. Ir de G a A (820 m): entregar paquete 3.
Camino: G -> F -> E -> B -> A
Todos los paquetes fueron recogidos y entregados.
Recorrido total: 2.44 km

 
Posición inicial: D
Paquetes: 30 Ubicaciones: 100
Calles: 224 (30 de un solo sentido)
1. En D: recoger paquete 18.
Camino: D
2. Ir de D a BE (620 m): recoger paquete 7.
Camino: D -> BT -> BE
3. Ir de BE a X (490 m): recoger paquete 12.
Camino: BE -> E -> X
4. Ir de X a AP (540 m): recoger paquete 5.
Camino: X -> B -> AP
5. Ir de AP a AT (350 m): recoger paquete 27.
Camino: AP -> AT
6. Ir de AT a AQ (200 m): recoger paquete 9.
Camino: AT -> AQ
7. Ir de AQ a BS (250 m): recoger paquete 25.
Camino: AQ -> BS
8. Ir de BS a CH (460 m): recoger paquete 10.
Camino: BS -> CT -> CJ -> CH
9. Ir de CH a CS (770 m): recoger paquete 19.
Camino: CH -> CJ -> CT -> BS -> CS
10. Ir de CS a AM (1160 m): recoger paquete 30.
Camino: CS -> AE -> AP -> AM
11. Ir de AM a AX (270 m): recoger paquete 17.
Camino: AM -> AK -> AX
12. Ir de AX a BW (1260 m): entregar paquete 30.
Camino: AX -> S -> K -> N -> BW
13. Ir de BW a CL (270 m): recoger paquete 14.
Camino: BW -> CL
14. Ir de CL a R (360 m): recoger paquetes 6 y 21.
Camino: CL -> R
15. Ir de R a BN (810 m): entregar paquete 7.
Camino: R -> CL -> BN
16. Ir de BN a BE (1190 m): entregar paquete 21.
Camino: BN -> CL -> R -> CL -> BW -> N -> K -> S -> AK -> AX -> AQ -> BS -> CT -> BE
17. Ir de BE a CM (730 m): entregar paquete 2.
Camino: BE -> E -> X -> AC -> M -> CM
18. Ir de CM a CU (1550 m): recoger paquete 15.
Camino: CM -> M -> AC -> CI -> Z -> CA -> BT -> CP -> BS -> CT -> CJ -> CV -> BX -> CU
19. Ir de CU a Z (1250 m): entregar paquete 12.
Camino: CU -> CV -> BX -> W -> CF -> T -> BE -> D -> Z
20. Ir de Z a BU (1370 m): recoger paquete 8.
Camino: Z -> D -> BT -> BE -> E -> X -> B -> BU
21. Ir de BU a CO (1190 m): recoger paquete 13.
Camino: BU -> AU -> BC -> A -> AL -> BP -> AM -> BU -> AU -> BC -> A -> AO -> CC -> AL -> BP -> AM -> AP -> AE -> AZ -> CO
22. Ir de CO a V (290 m): entregar paquete 1.
Camino: CO -> AZ -> V
23. Ir de V a Q (440 m): recoger paquete 22.
Camino: V -> O -> BY -> Q
24. Ir de Q a BJ (440 m): recoger paquete 29.
Camino: Q -> BJ
25. Ir de BJ a AG (1650 m): recoger paquete 16.
Camino: BJ -> Q -> BY -> O -> V -> AZ -> CO -> BL -> BH -> CG -> AS -> BF -> AG
26. Ir de AG a AI (430 m): recoger paquete 24.
Camino: AG -> BF -> AS -> AI
27. Ir de AI a AJ (1070 m): recoger paquete 26.
Camino: AI -> AS -> BH -> BQ -> BH -> BL -> CO -> AZ -> V -> O -> BY -> Q -> BJ -> CI -> CK -> CD -> BA -> AJ
28. Ir de AJ a BB (400 m): entregar paquete 16.
Camino: AJ -> BB
29. Ir de BB a AR (150 m): entregar paquete 8.
Camino: BB -> AR
30. Ir de AR a AD (280 m): entregar paquete 5.
Camino: AR -> AD
31. Ir de AD a CB (80 m): entregar paquete 3.
Camino: AD -> CB
32. Ir de CB a BK (1330 m): recoger paquete 11.
Camino: CB -> P -> BD -> CA -> BT -> BE -> E -> L -> AE -> AP -> AM -> BP -> AK -> BK
33. Ir de BK a CQ (100 m): entregar paquete 10.
Camino: BK -> CQ
34. Ir de CQ a BN (1660 m): entregar paquete 14.
Camino: CQ -> BK -> CT -> CJ -> CH -> U -> BO -> CJ -> CT -> BS -> AQ -> AX -> AK -> AM -> BP -> AL -> CC -> AO -> AI -> AS -> BM -> AS -> BH -> BL -> CO -> AZ -> V -> O -> BY -> Q -> BJ -> CI -> CK -> CD -> BA -> AG -> AU -> BC -> BU -> CN -> BU -> AU -> BC -> A -> AO -> CC -> AL -> BP -> AM -> AP -> AE -> CM -> M -> AC -> CI -> Z -> CA -> BT -> BE -> E -> L -> AE -> N -> BN
35. Ir de BN a BE (1190 m): entregar paquete 27.
Camino: BN -> CL -> R -> CL -> BW -> N -> K -> S -> AK -> AX -> AQ -> BS -> CT -> BE
36. Ir de BE a CV (1440 m): entregar paquete 25.
Camino: BE -> T -> CF -> W -> CV
37. Ir de CV a CU (540 m): recoger paquete 15.
Camino: CV -> BX -> CU
38. Ir de CU a M (1060 m): entregar paquete 1.
Camino: CU -> CV -> W -> CM -> M
39. Ir de M a AE (430 m): entregar paquete 26.
Camino: M -> CM -> AE
40. Ir de AE a AP (370 m): entregar paquetes 11 y 22.
Camino: AE -> AP
41. Ir de AP a AT (350 m): entregar paquetes 14 y 15.
Camino: AP -> AT
42. Ir de AT a AQ (200 m): entregar paquete 6.
Camino: AT -> AQ
43. Ir de AQ a BV (800 m): entregar paquete 19.
Camino: AQ -> AX -> F -> BV
44. Ir de BV a AB (470 m): recoger paquete 4.
Camino: BV -> AB
45. Ir de AB a CT (1620 m): entregar paquete 4.
Camino: AB -> BV -> F -> AX -> AQ -> BS -> CT
46. Ir de CT a CR (420 m): entregar paquete 20.
Camino: CT -> CR
Todos los paquetes fueron recogidos y entregados.
Recorrido total: 31.22 km