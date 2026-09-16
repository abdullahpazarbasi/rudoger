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
                        "text/plain": components["schemas"]["TokenResult"];
                        "application/json": components["schemas"]["TokenResult"];
                        "text/json": components["schemas"]["TokenResult"];
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
                        "text/plain": components["schemas"]["PageOfStockItemView"];
                        "application/json": components["schemas"]["PageOfStockItemView"];
                        "text/json": components["schemas"]["PageOfStockItemView"];
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
                        "text/plain": components["schemas"]["StockItemView"];
                        "application/json": components["schemas"]["StockItemView"];
                        "text/json": components["schemas"]["StockItemView"];
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
                        "text/plain": components["schemas"]["StockItemView"];
                        "application/json": components["schemas"]["StockItemView"];
                        "text/json": components["schemas"]["StockItemView"];
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
                        "text/plain": components["schemas"]["PageOfStockMovementView"];
                        "application/json": components["schemas"]["PageOfStockMovementView"];
                        "text/json": components["schemas"]["PageOfStockMovementView"];
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
                        "text/plain": components["schemas"]["StockMovementView"];
                        "application/json": components["schemas"]["StockMovementView"];
                        "text/json": components["schemas"]["StockMovementView"];
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
                        "text/plain": components["schemas"]["PageOfOrderView"];
                        "application/json": components["schemas"]["PageOfOrderView"];
                        "text/json": components["schemas"]["PageOfOrderView"];
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
                        "text/plain": components["schemas"]["OrderPlacementView"];
                        "application/json": components["schemas"]["OrderPlacementView"];
                        "text/json": components["schemas"]["OrderPlacementView"];
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
                        "text/plain": components["schemas"]["OrderView"];
                        "application/json": components["schemas"]["OrderView"];
                        "text/json": components["schemas"]["OrderView"];
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
                        "text/plain": components["schemas"]["OrderPlacementView"];
                        "application/json": components["schemas"]["OrderPlacementView"];
                        "text/json": components["schemas"]["OrderPlacementView"];
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
                        "text/plain": components["schemas"]["OrderTransitionView"];
                        "application/json": components["schemas"]["OrderTransitionView"];
                        "text/json": components["schemas"]["OrderTransitionView"];
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
                        "text/plain": components["schemas"]["OrderTransitionView"];
                        "application/json": components["schemas"]["OrderTransitionView"];
                        "text/json": components["schemas"]["OrderTransitionView"];
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
                        "text/plain": components["schemas"]["PageOfProductView"];
                        "application/json": components["schemas"]["PageOfProductView"];
                        "text/json": components["schemas"]["PageOfProductView"];
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
                        "text/plain": components["schemas"]["ProductView"];
                        "application/json": components["schemas"]["ProductView"];
                        "text/json": components["schemas"]["ProductView"];
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
                        "text/plain": components["schemas"]["ProductView"];
                        "application/json": components["schemas"]["ProductView"];
                        "text/json": components["schemas"]["ProductView"];
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
                        "text/plain": components["schemas"]["ProductView"];
                        "application/json": components["schemas"]["ProductView"];
                        "text/json": components["schemas"]["ProductView"];
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
                        "text/plain": components["schemas"]["ProductPackagingView"][];
                        "application/json": components["schemas"]["ProductPackagingView"][];
                        "text/json": components["schemas"]["ProductPackagingView"][];
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
                        "text/plain": components["schemas"]["ProductPackagingView"];
                        "application/json": components["schemas"]["ProductPackagingView"];
                        "text/json": components["schemas"]["ProductPackagingView"];
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
                        "text/plain": components["schemas"]["ProductPackagingView"];
                        "application/json": components["schemas"]["ProductPackagingView"];
                        "text/json": components["schemas"]["ProductPackagingView"];
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
                        "text/plain": components["schemas"]["ProductPackagingView"];
                        "application/json": components["schemas"]["ProductPackagingView"];
                        "text/json": components["schemas"]["ProductPackagingView"];
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
        CreateOrderRequest: {
            lines: components["schemas"]["OrderPlacementLineRequest"][];
        };
        CreateOrderTransitionRequest: {
            target: components["schemas"]["OrderTransitionTarget"];
        };
        CreateProductRequest: {
            sku: string;
            name: string;
            baseUomCode: string;
            /** Format: double */
            basePriceAmount: number | string;
            basePriceCurrencyCode: string;
            packagings: components["schemas"]["ProductPackagingRequest"][];
        };
        CreateStockItemRequest: {
            /** Format: uuid */
            productId: string;
            /** Format: double */
            openingQuantity: number | string;
        };
        CreateStockMovementRequest: {
            type: components["schemas"]["StockMovementType"];
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
        OrderLineView: {
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
        OrderPlacementLineView: {
            /** Format: int32 */
            num: number | string;
            /** Format: uuid */
            productId: string;
            uomCode: string;
            /** Format: double */
            quantity: number | string;
        };
        OrderPlacementStatus: number;
        OrderPlacementView: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            orderId: string;
            /** Format: uuid */
            userId: string;
            status: components["schemas"]["OrderPlacementStatus"];
            lines: components["schemas"]["OrderPlacementLineView"][];
            failureCode: null | string;
            failureDetail: null | string;
        };
        OrderStatus: number;
        OrderTransitionStatus: number;
        OrderTransitionTarget: number;
        OrderTransitionView: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            orderId: string;
            target: components["schemas"]["OrderTransitionTarget"];
            status: components["schemas"]["OrderTransitionStatus"];
        };
        OrderView: {
            /** Format: uuid */
            id: string;
            orderNumber: string;
            status: components["schemas"]["OrderStatus"];
            /** Format: uuid */
            userId: string;
            lines: components["schemas"]["OrderLineView"][];
            /** Format: uuid */
            pendingTransitionId: null | string;
            pendingTransitionTarget: null | components["schemas"]["OrderTransitionTarget"];
        };
        PageOfOrderView: {
            items: components["schemas"]["OrderView"][];
            /** Format: int32 */
            pageNumber: number | string;
            /** Format: int32 */
            pageSize: number | string;
            /** Format: int32 */
            totalCount: number | string;
        };
        PageOfProductView: {
            items: components["schemas"]["ProductView"][];
            /** Format: int32 */
            pageNumber: number | string;
            /** Format: int32 */
            pageSize: number | string;
            /** Format: int32 */
            totalCount: number | string;
        };
        PageOfStockItemView: {
            items: components["schemas"]["StockItemView"][];
            /** Format: int32 */
            pageNumber: number | string;
            /** Format: int32 */
            pageSize: number | string;
            /** Format: int32 */
            totalCount: number | string;
        };
        PageOfStockMovementView: {
            items: components["schemas"]["StockMovementView"][];
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
        ProductPackagingView: {
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
        ProductView: {
            /** Format: uuid */
            id: string;
            sku: string;
            name: string;
            baseUomCode: string;
            /** Format: double */
            basePriceAmount: number | string;
            basePriceCurrencyCode: string;
            packagings: components["schemas"]["ProductPackagingView"][];
        };
        StockItemView: {
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
        StockMovementType: number;
        StockMovementView: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            stockItemId: string;
            type: components["schemas"]["StockMovementType"];
            /** Format: double */
            onHandQuantityDelta: number | string;
            /** Format: double */
            reservedQuantityDelta: number | string;
            referenceType: string;
            /** Format: uuid */
            referenceId: null | string;
            idempotencyKey: string;
            correlationId: string;
            /** Format: uuid */
            sourceEventId: string;
            /** Format: date-time */
            occurredAtUtc: string;
        };
        TokenExchangeRequest: {
            username: string;
            password: string;
        };
        TokenResult: {
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
