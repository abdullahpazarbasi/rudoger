export interface paths {
    "/api/v1/authn/tokens": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["TokenExchangeRequest"];
                    "text/json": components["schemas"]["TokenExchangeRequest"];
                    "application/*+json": components["schemas"]["TokenExchangeRequest"];
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["TokenResponse"];
                        "application/json": components["schemas"]["TokenResponse"];
                        "text/json": components["schemas"]["TokenResponse"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/inventory/stock-items": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: {
                    productId?: string;
                    pageNumber?: number | string;
                    pageSize?: number | string;
                };
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["PageResponseOfStockItemResponse"];
                        "application/json": components["schemas"]["PageResponseOfStockItemResponse"];
                        "text/json": components["schemas"]["PageResponseOfStockItemResponse"];
                    };
                };
            };
        };
        put?: never;
        post: {
            parameters: {
                query?: never;
                header?: {
                    "Idempotency-Key"?: string;
                };
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["CreateStockItemRequest"];
                    "text/json": components["schemas"]["CreateStockItemRequest"];
                    "application/*+json": components["schemas"]["CreateStockItemRequest"];
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["StockItemResponse"];
                        "application/json": components["schemas"]["StockItemResponse"];
                        "text/json": components["schemas"]["StockItemResponse"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/inventory/stock-items/{stockItemId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    stockItemId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["StockItemResponse"];
                        "application/json": components["schemas"]["StockItemResponse"];
                        "text/json": components["schemas"]["StockItemResponse"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/inventory/stock-items/{stockItemId}/movements": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: {
                    pageNumber?: number | string;
                    pageSize?: number | string;
                };
                header?: never;
                path: {
                    stockItemId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["PageResponseOfStockMovementResponse"];
                        "application/json": components["schemas"]["PageResponseOfStockMovementResponse"];
                        "text/json": components["schemas"]["PageResponseOfStockMovementResponse"];
                    };
                };
            };
        };
        put?: never;
        post: {
            parameters: {
                query?: never;
                header?: {
                    "Idempotency-Key"?: string;
                };
                path: {
                    stockItemId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["CreateStockMovementRequest"];
                    "text/json": components["schemas"]["CreateStockMovementRequest"];
                    "application/*+json": components["schemas"]["CreateStockMovementRequest"];
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["StockMovementResponse"];
                        "application/json": components["schemas"]["StockMovementResponse"];
                        "text/json": components["schemas"]["StockMovementResponse"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/order/orders": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: {
                    pageNumber?: number | string;
                    pageSize?: number | string;
                };
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["PageResponseOfOrderResponse"];
                        "application/json": components["schemas"]["PageResponseOfOrderResponse"];
                        "text/json": components["schemas"]["PageResponseOfOrderResponse"];
                    };
                };
            };
        };
        put?: never;
        post: {
            parameters: {
                query?: never;
                header?: {
                    "Idempotency-Key"?: string;
                };
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["CreateOrderRequest"];
                    "text/json": components["schemas"]["CreateOrderRequest"];
                    "application/*+json": components["schemas"]["CreateOrderRequest"];
                };
            };
            responses: {
                /** @description Accepted */
                202: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["OrderPlacementResponse"];
                        "application/json": components["schemas"]["OrderPlacementResponse"];
                        "text/json": components["schemas"]["OrderPlacementResponse"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/order/orders/{orderId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    orderId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["OrderResponse"];
                        "application/json": components["schemas"]["OrderResponse"];
                        "text/json": components["schemas"]["OrderResponse"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/order/order-placements/{placementId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    placementId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["OrderPlacementResponse"];
                        "application/json": components["schemas"]["OrderPlacementResponse"];
                        "text/json": components["schemas"]["OrderPlacementResponse"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/order/orders/{orderId}/transitions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    orderId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["CreateOrderTransitionRequest"];
                    "text/json": components["schemas"]["CreateOrderTransitionRequest"];
                    "application/*+json": components["schemas"]["CreateOrderTransitionRequest"];
                };
            };
            responses: {
                /** @description Accepted */
                202: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["OrderTransitionResponse"];
                        "application/json": components["schemas"]["OrderTransitionResponse"];
                        "text/json": components["schemas"]["OrderTransitionResponse"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/order/orders/{orderId}/transitions/{transitionId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    orderId: string;
                    transitionId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["OrderTransitionResponse"];
                        "application/json": components["schemas"]["OrderTransitionResponse"];
                        "text/json": components["schemas"]["OrderTransitionResponse"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/product/products": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: {
                    ids?: string[];
                    pageNumber?: number | string;
                    pageSize?: number | string;
                };
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["PageResponseOfProductResponse"];
                        "application/json": components["schemas"]["PageResponseOfProductResponse"];
                        "text/json": components["schemas"]["PageResponseOfProductResponse"];
                    };
                };
            };
        };
        put?: never;
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["CreateProductRequest"];
                    "text/json": components["schemas"]["CreateProductRequest"];
                    "application/*+json": components["schemas"]["CreateProductRequest"];
                };
            };
            responses: {
                /** @description Created */
                201: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["ProductResponse"];
                        "application/json": components["schemas"]["ProductResponse"];
                        "text/json": components["schemas"]["ProductResponse"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/product/products/{productId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    productId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["ProductResponse"];
                        "application/json": components["schemas"]["ProductResponse"];
                        "text/json": components["schemas"]["ProductResponse"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    productId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
            };
        };
        options?: never;
        head?: never;
        patch: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    productId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json-patch+json": components["schemas"]["JsonPatchDocument"];
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["ProductResponse"];
                        "application/json": components["schemas"]["ProductResponse"];
                        "text/json": components["schemas"]["ProductResponse"];
                    };
                };
            };
        };
        trace?: never;
    };
    "/api/v1/product/products/{productId}/packagings": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    productId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["ProductPackagingResponse"][];
                        "application/json": components["schemas"]["ProductPackagingResponse"][];
                        "text/json": components["schemas"]["ProductPackagingResponse"][];
                    };
                };
            };
        };
        put?: never;
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    productId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["ProductPackagingRequest"];
                    "text/json": components["schemas"]["ProductPackagingRequest"];
                    "application/*+json": components["schemas"]["ProductPackagingRequest"];
                };
            };
            responses: {
                /** @description Created */
                201: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["ProductPackagingResponse"];
                        "application/json": components["schemas"]["ProductPackagingResponse"];
                        "text/json": components["schemas"]["ProductPackagingResponse"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/product/products/{productId}/packagings/{packagingId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    productId: string;
                    packagingId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["ProductPackagingResponse"];
                        "application/json": components["schemas"]["ProductPackagingResponse"];
                        "text/json": components["schemas"]["ProductPackagingResponse"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    productId: string;
                    packagingId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
            };
        };
        options?: never;
        head?: never;
        patch: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    productId: string;
                    packagingId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json-patch+json": components["schemas"]["JsonPatchDocument"];
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["ProductPackagingResponse"];
                        "application/json": components["schemas"]["ProductPackagingResponse"];
                        "text/json": components["schemas"]["ProductPackagingResponse"];
                    };
                };
            };
        };
        trace?: never;
    };
}
export type webhooks = Record<string, never>;
export interface components {
    schemas: {
        /**
         * @example {
         *       "lines": [
         *         {
         *           "productId": "00000000-0000-0000-0000-000000000000",
         *           "uomCode": "CASE",
         *           "quantity": 2
         *         }
         *       ]
         *     }
         */
        CreateOrderRequest: {
            lines: components["schemas"]["OrderPlacementLineRequest"][];
        };
        /**
         * @example {
         *       "target": "Shipped"
         *     }
         */
        CreateOrderTransitionRequest: {
            target: components["schemas"]["OrderTransitionTargetContract"];
        };
        /**
         * @example {
         *       "sku": "COFFEE-001",
         *       "name": "Coffee",
         *       "baseUomCode": "EA",
         *       "basePriceAmount": 2.5,
         *       "basePriceCurrencyCode": "TRY",
         *       "packagings": [
         *         {
         *           "level": 0,
         *           "uomCode": "EA",
         *           "conversionFactor": 1,
         *           "barcode": "869000000001"
         *         },
         *         {
         *           "level": 1,
         *           "uomCode": "CASE",
         *           "conversionFactor": 10,
         *           "barcode": "869000000010"
         *         }
         *       ]
         *     }
         */
        CreateProductRequest: {
            sku: string;
            name: string;
            baseUomCode: string;
            /** Format: double */
            basePriceAmount: number | string;
            basePriceCurrencyCode: string;
            packagings: components["schemas"]["ProductPackagingRequest"][];
        };
        /**
         * @example {
         *       "productId": "00000000-0000-0000-0000-000000000000",
         *       "uomCode": "CASE",
         *       "openingQuantity": 10
         *     }
         */
        CreateStockItemRequest: {
            /** Format: uuid */
            productId: string;
            uomCode: string;
            /** Format: double */
            openingQuantity: number | string;
        };
        /**
         * @example {
         *       "type": "Receipt",
         *       "uomCode": "CASE",
         *       "quantity": 2
         *     }
         */
        CreateStockMovementRequest: {
            type: components["schemas"]["StockMovementTypeContract"];
            uomCode: string;
            /** Format: double */
            quantity: number | string;
        };
        JsonPatchDocument: ({
            /** @enum {string} */
            op: "add" | "replace" | "test";
            path: string;
            value: unknown;
        } | {
            /** @enum {string} */
            op: "move" | "copy";
            path: string;
            from: string;
        } | {
            /** @enum {string} */
            op: "remove";
            path: string;
        })[];
        OrderLineResponse: {
            /** Format: uuid */
            id: string;
            /** Format: int32 */
            num: number | string;
            /** Format: uuid */
            productId: string;
            uomCode: string;
            /** Format: double */
            quantity: number | string;
            /** Format: double */
            unitPriceAmount: number | string;
            unitPriceCurrencyCode: string;
        };
        OrderPlacementLineRequest: {
            /** Format: uuid */
            productId: string;
            uomCode: string;
            /** Format: double */
            quantity: number | string;
        };
        OrderPlacementLineResponse: {
            /** Format: int32 */
            num: number | string;
            /** Format: uuid */
            productId: string;
            uomCode: string;
            /** Format: double */
            quantity: number | string;
        };
        OrderPlacementResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            orderId: string;
            /** Format: uuid */
            userId: string;
            status: components["schemas"]["OrderPlacementStatusContract"];
            lines: components["schemas"]["OrderPlacementLineResponse"][];
            failureCode: null | string;
            failureDetail: null | string;
        };
        OrderPlacementStatusContract: number;
        OrderResponse: {
            /** Format: uuid */
            id: string;
            orderNumber: string;
            status: components["schemas"]["OrderStatusContract"];
            /** Format: uuid */
            userId: string;
            lines: components["schemas"]["OrderLineResponse"][];
            /** Format: uuid */
            pendingTransitionId: null | string;
            pendingTransitionTarget: null | components["schemas"]["OrderTransitionTargetContract"];
        };
        OrderStatusContract: number;
        OrderTransitionResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            orderId: string;
            target: components["schemas"]["OrderTransitionTargetContract"];
            status: components["schemas"]["OrderTransitionStatusContract"];
        };
        OrderTransitionStatusContract: number;
        OrderTransitionTargetContract: number;
        PageResponseOfOrderResponse: {
            items: components["schemas"]["OrderResponse"][];
            /** Format: int32 */
            pageNumber: number | string;
            /** Format: int32 */
            pageSize: number | string;
            /** Format: int32 */
            totalCount: number | string;
        };
        PageResponseOfProductResponse: {
            items: components["schemas"]["ProductResponse"][];
            /** Format: int32 */
            pageNumber: number | string;
            /** Format: int32 */
            pageSize: number | string;
            /** Format: int32 */
            totalCount: number | string;
        };
        PageResponseOfStockItemResponse: {
            items: components["schemas"]["StockItemResponse"][];
            /** Format: int32 */
            pageNumber: number | string;
            /** Format: int32 */
            pageSize: number | string;
            /** Format: int32 */
            totalCount: number | string;
        };
        PageResponseOfStockMovementResponse: {
            items: components["schemas"]["StockMovementResponse"][];
            /** Format: int32 */
            pageNumber: number | string;
            /** Format: int32 */
            pageSize: number | string;
            /** Format: int32 */
            totalCount: number | string;
        };
        ProductPackagingRequest: {
            /** Format: uuid */
            id: null | string;
            /** Format: int32 */
            level: number | string;
            uomCode: string;
            /** Format: double */
            conversionFactor: number | string;
            barcode: null | string;
            /** Format: double */
            weightInKg: null | number | string;
            /** Format: double */
            lengthInMm: null | number | string;
            /** Format: double */
            widthInMm: null | number | string;
            /** Format: double */
            heightInMm: null | number | string;
        };
        ProductPackagingResponse: {
            /** Format: uuid */
            id: string;
            /** Format: int32 */
            level: number | string;
            uomCode: string;
            /** Format: double */
            conversionFactor: number | string;
            barcode: null | string;
            /** Format: double */
            weightInKg: null | number | string;
            /** Format: double */
            lengthInMm: null | number | string;
            /** Format: double */
            widthInMm: null | number | string;
            /** Format: double */
            heightInMm: null | number | string;
        };
        ProductResponse: {
            /** Format: uuid */
            id: string;
            sku: string;
            name: string;
            baseUomCode: string;
            /** Format: double */
            basePriceAmount: number | string;
            basePriceCurrencyCode: string;
            packagings: components["schemas"]["ProductPackagingResponse"][];
        };
        StockItemResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            productId: string;
            baseUomCode: string;
            /** Format: double */
            onHandQuantity: number | string;
            /** Format: double */
            reservedQuantity: number | string;
            /** Format: double */
            availableQuantity: number | string;
        };
        StockMovementResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            stockItemId: string;
            type: components["schemas"]["StockMovementTypeContract"];
            uomCode: string;
            /** Format: double */
            quantity: number | string;
            /** Format: double */
            onHandQuantityDelta: number | string;
            /** Format: double */
            reservedQuantityDelta: number | string;
            referenceType: string;
            /** Format: uuid */
            referenceId: null | string;
            idempotencyKey: string;
            correlationId: string;
            /** Format: date-time */
            occurredAtUtc: string;
        };
        StockMovementTypeContract: number;
        /**
         * @example {
         *       "username": "abdullah",
         *       "password": "12345678"
         *     }
         */
        TokenExchangeRequest: {
            username: string;
            password: string;
        };
        TokenResponse: {
            accessToken: string;
            tokenType: string;
            /** Format: int32 */
            expiresIn: number | string;
        };
    };
    responses: never;
    parameters: never;
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export type operations = Record<string, never>;
