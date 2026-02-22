import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SecureStorageService {
  private encoder = new TextEncoder();
  private decoder = new TextDecoder();
  private keyPromise: Promise<CryptoKey | null>;
  private prefix = '__encrypted_';
  private cryptoAvailable = false;

  constructor() {
    // Check if crypto.subtle is available (requires secure context)
    if (typeof crypto !== 'undefined' && crypto.subtle) {
      this.cryptoAvailable = true;
      this.keyPromise = this.deriveKey(environment.encryptionKey).catch((err) => {
        console.warn('SecureStorageService: Failed to derive encryption key, falling back to plain storage', err);
        this.cryptoAvailable = false;
        return null;
      });
    } else {
      console.warn('SecureStorageService: crypto.subtle not available, using plain storage');
      this.keyPromise = Promise.resolve(null);
    }
  }

  async setItem(key: string, value: unknown, useSession = false): Promise<void> {
    const storage = this.getStorage(useSession);
    const payload = typeof value === 'string' ? value : JSON.stringify(value);
    
    const cryptoKey = await this.keyPromise;
    
    // Fallback to plain storage if crypto is not available
    if (!cryptoKey || !this.cryptoAvailable) {
      storage.setItem(key, payload);
      return;
    }

    try {
      // Encrypt both key and value together as a JSON object
      const plainData = JSON.stringify({ key, value: payload });
      const iv = crypto.getRandomValues(new Uint8Array(12));
      const cipherBuffer = await crypto.subtle.encrypt(
        { name: 'AES-GCM', iv },
        cryptoKey,
        this.encoder.encode(plainData)
      );
      
      // Store with a hashed key lookup for retrieval
      const hashedKey = this.hashKey(key);
      const cipherArray = new Uint8Array(cipherBuffer);
      const packed = this.pack(iv, cipherArray);
      
      storage.setItem(this.prefix + hashedKey, packed);
    } catch (error) {
      // Fallback to plain storage on encryption error
      console.warn('SecureStorageService: Encryption failed, using plain storage', error);
      storage.setItem(key, payload);
    }
  }

  async getItem<T>(key: string, useSession = false): Promise<T | null> {
    const storage = this.getStorage(useSession);
    
    const cryptoKey = await this.keyPromise;
    
    // Try plain storage first (for fallback or migration)
    const plainValue = storage.getItem(key);
    if (plainValue !== null) {
      return this.deserialize<T>(plainValue);
    }
    
    // If crypto not available, we already checked plain storage
    if (!cryptoKey || !this.cryptoAvailable) {
      return null;
    }
    
    // Try encrypted storage
    const hashedKey = this.hashKey(key);
    const stored = storage.getItem(this.prefix + hashedKey);
    
    if (!stored) {
      return null;
    }

    try {
      const { iv, data } = this.unpack(stored);
      // @ts-ignore - TypeScript strictness issue with SharedArrayBuffer vs ArrayBuffer in Uint8Array
      const plainBuffer = await crypto.subtle.decrypt({ name: 'AES-GCM', iv }, cryptoKey, data);
      const decryptedText = this.decoder.decode(plainBuffer);
      const decrypted = JSON.parse(decryptedText);
      
      // Verify the key matches (prevent key/value mismatches)
      if (decrypted.key !== key) {
        return null;
      }
      
      return this.deserialize<T>(decrypted.value);
    } catch (error) {
      console.warn('SecureStorageService: Decryption failed', error);
      return null;
    }
  }

  removeItem(key: string, useSession = false): void {
    const storage = this.getStorage(useSession);
    // Remove both plain and encrypted versions
    storage.removeItem(key);
    const hashedKey = this.hashKey(key);
    storage.removeItem(this.prefix + hashedKey);
  }

  clear(useSession = false): void {
    const storage = this.getStorage(useSession);
    const keysToRemove: string[] = [];
    
    for (let i = 0; i < storage.length; i++) {
      const storageKey = storage.key(i);
      if (storageKey?.startsWith(this.prefix)) {
        keysToRemove.push(storageKey);
      }
    }
    
    keysToRemove.forEach(k => storage.removeItem(k));
  }

  private getStorage(useSession: boolean): Storage {
    return useSession ? sessionStorage : localStorage;
  }

  private async deriveKey(secret: string): Promise<CryptoKey> {
    const normalized = secret || '';
    const hashed = await crypto.subtle.digest('SHA-256', this.encoder.encode(normalized));
    return crypto.subtle.importKey('raw', hashed, { name: 'AES-GCM' }, false, ['encrypt', 'decrypt']);
  }

  private hashKey(key: string): string {
    // Simple hash to create storage key (not cryptographic, just for lookup)
    let hash = 0;
    for (let i = 0; i < key.length; i++) {
      const char = key.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32bit integer
    }
    return Math.abs(hash).toString(36);
  }

  private pack(iv: Uint8Array, data: Uint8Array): string {
    const ivB64 = this.toBase64(iv);
    const dataB64 = this.toBase64(data);
    return `${ivB64}:${dataB64}`;
  }

  private unpack(payload: string): { iv: Uint8Array; data: Uint8Array } {
    const [ivB64, dataB64] = payload.split(':');
    if (!ivB64 || !dataB64) {
      throw new Error('Invalid payload format');
    }
    return { iv: this.fromBase64(ivB64), data: this.fromBase64(dataB64) };
  }

  private toBase64(bytes: Uint8Array): string {
    let binary = '';
    bytes.forEach((b) => (binary += String.fromCharCode(b)));
    return btoa(binary);
  }

  private fromBase64(value: string): Uint8Array {
    const binary = atob(value);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i);
    }
    return bytes;
  }

  private deserialize<T>(text: string): T {
    try {
      return JSON.parse(text) as T;
    } catch {
      return text as unknown as T;
    }
  }
}
