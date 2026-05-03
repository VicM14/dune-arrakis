# Dune: Arrakis Dominion Distributed

Este proyecto es una simulación distribuida de gestión logística y biológica en el universo de Dune, desarrollada para la asignatura de Programación en Entornos Distribuidos (2026).

## Arquitectura del Sistema

El sistema sigue una arquitectura de **Microservicios desacoplados** comunicados mediante **REST API**, lo que permite escalabilidad y separación de responsabilidades:

1.  **Dune.Domain (Modelo Compartido):** Biblioteca de clases que contiene las entidades de negocio (`Criatura`, `Enclave`), enumerados y DTOs. Es el "lenguaje común" que entienden todos los componentes.
2.  **Dune.SimulationService (Cerebro):** Servicio ASP.NET Core que procesa la lógica de las rondas mensuales, cálculos de salud y algoritmos de visitantes.
3.  **Dune.PersistenceService (Memoria):** Servicio encargado de la serialización JSON y el almacenamiento persistente del estado de la partida.
4.  **Dune.UnityClient (Interfaz):** Cliente desarrollado en Unity que actúa como "Centro de Mando", permitiendo al usuario visualizar el estado y enviar órdenes al servidor.

## Estrategia de Comunicación

*   **Protocolo:** HTTP/1.1 con mensajes en formato **JSON**.
*   **Interacción:** Síncrona mediante peticiones `GET` para consulta de estado y `POST` para ejecución de acciones (como pasar de mes).
*   **Seguridad:** Implementación de políticas **CORS** en los servicios para permitir la conexión segura desde el cliente Unity.

## Tecnologías Utilizadas
*   **Backend:** .NET 8.0 (Minimal APIs)
*   **Frontend:** Unity 2022.3+
*   **Persistencia:** System.Text.Json
*   **Control de Versiones:** Git / GitHub

Se ha implementado un sistema mínimamente ejecutable que cumple con los requisitos de creación, almacenamiento y recuperación de información:

### Componentes Funcionales:
*   **Dune.AdminClient:** Cliente de consola capaz de generar una partida inicial y enviar la orden de guardado al ecosistema distribuido.
*   **Dune.SimulationService:** Actúa como orquestador, recibiendo las peticiones del cliente y delegando la responsabilidad de almacenamiento al servicio especializado.
*   **Dune.PersistenceService:** Módulo de persistencia que gestiona la serialización y escritura de datos en formato **JSON** (`partida_arrakis.json`).

### Mecanismo de Comunicación:
*   Se ha establecido un flujo de comunicación **Service-to-Service** utilizando `HttpClient` y el protocolo HTTP.
*   La arquitectura permite el desacoplamiento total: el cliente no sabe dónde ni cómo se guardan los datos, solo conoce el endpoint de simulación.

### Cómo probar la funcionalidad:
1. Iniciar la solución con múltiples proyectos de inicio (`SimulationService` y `PersistenceService`).
2. Ejecutar el `AdminClient`.
3. Verificar la creación del archivo `partida_arrakis.json` en la carpeta del servicio de persistencia.

