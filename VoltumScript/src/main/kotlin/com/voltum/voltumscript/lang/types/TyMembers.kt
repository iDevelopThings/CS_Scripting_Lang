package com.voltum.voltumscript.lang.types

import com.intellij.psi.stubs.StubOutputStream
import com.voltum.voltumscript.psi.*

enum class TyFieldKind {
    FIELD,
    METHOD
}

class TyField {
    private var _onResolve: (TyField, Ty?) -> Unit

    val owner: Ty
    val name: String

    private var _ty: Lazy<Ty?> = lazy {
        null
    }

    var ty: Ty?
        get() {
            val ty = _ty.value
            _onResolve(this, ty)
            return ty
        }
        set(value) {
            _ty = lazy { value }
            _onResolve(this, value)
        }
    
    val lazyTy: Lazy<Ty?> get() = _ty

    val kind: TyFieldKind

    constructor(owner: Ty, name: String, ty: Ty?, kind: TyFieldKind = TyFieldKind.FIELD, onResolve: (TyField, Ty?) -> Unit = { _, _ -> }) {
        this.owner = owner
        this.name = name
        this._ty = ty?.let { lazy { it } } ?: lazy { null }
        this.kind = kind
        this._onResolve = onResolve
    }

    constructor(owner: Ty, name: String, ty: Lazy<Ty?>, kind: TyFieldKind = TyFieldKind.FIELD, onResolve: (TyField, Ty?) -> Unit = { _, _ -> }) {
        this.owner = owner
        this.name = name
        this._ty = ty
        this.kind = kind
        this._onResolve = onResolve
    }

    var linkedElement: VoltumElement? = null

    fun serialize(dataStream: StubOutputStream) {
        dataStream.writeInt(owner.id)
        dataStream.writeUTFFast(name)
        dataStream.writeInt(ty?.id ?: -1)
    }

    /*fun deserialize(dataStream: StubInputStream): TyField {
        return TyField(
            owner = TyRegistry[dataStream.readInt()]!!,
            )
    }*/
}
