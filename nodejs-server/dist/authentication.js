"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.validateToken = void 0;
const validateToken = (token) => {
    // TODO: decode token to get user id and expire time
    // if token is expire return false
    // find user with user id
    // or valid steam token with steam sdk
    if (!token)
        return null;
    let userInfo = {
        userId: token,
        name: token,
    };
    return userInfo;
};
exports.validateToken = validateToken;
