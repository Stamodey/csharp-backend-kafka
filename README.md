# C# Backend — Order Processing System

Backend-система для обработки заказов с использованием **C# / .NET, Apache Kafka, PostgreSQL и Redis**.

Проект реализует REST API для работы с заказами и асинхронное взаимодействие между сервисами через Kafka. Система разделена на несколько компонентов и запускается локально с помощью Docker Compose.

## Основные возможности

* создание и изменение заказов;
* получение информации о заказах;
* изменение статусов заказов;
* аудит изменений;
* асинхронная передача событий через Apache Kafka;
* обработка Kafka-сообщений consumer-сервисами;
* batch-обработка сообщений;
* валидация входных данных;
* миграции PostgreSQL;
* взаимодействие между сервисами через HTTP;
* кэширование и работа с Redis;
* контейнеризированный запуск компонентов.

## Архитектура

Проект состоит из нескольких компонентов:

```text
                         ┌─────────────────┐
                         │     Client      │
                         └────────┬────────┘
                                  │ HTTP
                                  ▼
                         ┌─────────────────┐
                         │     WebApi      │
                         │   ASP.NET Core  │
                         └───────┬─┬───────┘
                                 │ │
                     ┌───────────┘ └───────────┐
                     ▼                         ▼
              ┌─────────────┐           ┌─────────────┐
              │ PostgreSQL  │           │    Redis    │
              └─────────────┘           └─────────────┘
                                  
                         ┌─────────────────┐
                         │ Apache Kafka    │
                         └────────┬────────┘
                                  │
                    ┌─────────────┴─────────────┐
                    ▼                           ▼
             ┌─────────────┐             ┌─────────────┐
             │  Consumer   │             │ Batch       │
             │  Service    │             │ Consumers   │
             └─────────────┘             └─────────────┘
```

### WebApi

Основной HTTP-сервис приложения.

Включает:

* ASP.NET Core Web API;
* Controllers;
* Business Logic Layer;
* Data Access Layer;
* Repository pattern;
* Unit of Work;
* FluentValidation;
* Kafka Producer;
* PostgreSQL integration.

### Consumer

Отдельный сервис для обработки событий из Kafka.

Реализованы:

* базовый Kafka Consumer;
* batch consumer;
* обработчики событий создания заказа;
* обработчики изменения статуса заказа;
* взаимодействие с внешним OMS-сервисом.

### Messages

Общие модели сообщений, используемые для взаимодействия между сервисами через Kafka.

### Migrations

Компонент для управления миграциями базы данных PostgreSQL.

## Технологический стек

### Backend

* C#
* .NET / ASP.NET Core
* REST API
* gRPC
* async/await
* `Task`
* `Task.WhenAll`
* `CancellationToken`

### Messaging

* Apache Kafka
* Confluent.Kafka
* Kafka Producer / Consumer
* batch processing
* idempotent producer

Для producer используются настройки, ориентированные на надёжную доставку сообщений:

```text
Acks = All
EnableIdempotence = true
MaxInFlight = 5
LingerMs = 10
BatchSize = 65536
CompressionType = Zstd
```

### Databases

* PostgreSQL
* Redis

### Infrastructure

* Docker
* Docker Compose
* Linux

## Структура проекта

```text
OrdersSolution
│
├── Common/
│   └── Общие вспомогательные компоненты
│
├── Consumer/
│   ├── Base/
│   ├── Clients/
│   ├── Config/
│   └── Consumers/
│
├── Messages/
│   └── Kafka message contracts
│
├── Migrations/
│   └── Database migrations
│
├── Models/
│   └── DTO и общие модели
│
├── WebApi/
│   ├── BLL/
│   │   ├── Models/
│   │   └── Services/
│   ├── DAL/
│   │   ├── Interfaces/
│   │   ├── Models/
│   │   └── Repositories/
│   ├── Controllers/
│   ├── Jobs/
│   └── Validators/
│
├── docker-compose.yml
├── postgresql.conf
└── OrdersSolution.sln
```

## Kafka events

В системе используются события, связанные с жизненным циклом заказа.

Основные topics:

```text
oms_order_created
oms_order_status_changed
```

Поток обработки события:

```text
WebApi
  │
  │ publish
  ▼
Kafka
  │
  │ consume
  ▼
Consumer
  │
  ▼
OMS / business logic
```

Это позволяет отделить HTTP-запрос от дальнейшей асинхронной обработки события.

## Database

Для хранения данных используется PostgreSQL.

Миграции находятся в:

```text
Migrations/Scripts/
```

В проекте реализованы миграции для:

* создания таблицы заказов;
* создания таблицы аудита;
* добавления статуса заказа.

## Запуск

### Требования

Перед запуском необходимо установить:

* Docker
* Docker Compose
* .NET SDK, если требуется запуск отдельных компонентов вне контейнеров.

### Запуск через Docker Compose

Клонировать репозиторий:

```bash
git clone https://github.com/Stamodey/csharp-backend-kafka.git
cd csharp-backend-kafka
```

Запустить сервисы:

```bash
docker compose up -d --build
```

Проверить состояние контейнеров:

```bash
docker compose ps
```

Посмотреть логи:

```bash
docker compose logs -f
```

Остановить систему:

```bash
docker compose down
```

## API

Основные операции API связаны с жизненным циклом заказа:

```text
POST   /api/v1/orders
GET    /api/v1/orders
PUT    /api/v1/orders/status
GET    /api/v1/audit-log-orders
```

Точные маршруты и модели запросов находятся в:

```text
WebApi/Controllers/V1/
Models/Dto/V1/
```

## Что демонстрирует проект

Проект демонстрирует практическую работу с backend-разработкой на C#/.NET:

* построение многослойной архитектуры;
* разработку REST API;
* работу с PostgreSQL;
* repository и Unit of Work;
* асинхронное программирование;
* Kafka producer/consumer;
* обработку сообщений;
* batch processing;
* идемпотентную публикацию сообщений;
* межсервисное взаимодействие;
* Docker Compose;
* миграции базы данных;
* валидацию API-запросов.

## License

Проект предназначен для учебных и демонстрационных целей.
